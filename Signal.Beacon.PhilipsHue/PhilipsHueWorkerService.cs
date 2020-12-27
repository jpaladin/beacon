using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.PhilipsHue
{
    public class PhilipsHueWorkerService : IWorkerService
    {
        private readonly ICommandHandler<DeviceStateSetCommand> deviceStateSerHandler;
        private readonly ICommandHandler<DeviceDiscoveredCommand> deviceDiscoveryHandler;
        private readonly IDevicesDao devicesDao;
        private readonly ILogger<PhilipsHueWorkerService> logger;
        private readonly IConfigurationService configurationService;

        private const int RegisterBridgeRetryTimes = 5;

        private readonly List<BridgeConnection> bridges = new();
        private readonly Dictionary<string, Light> lights = new();

        public PhilipsHueWorkerService(
            ICommandHandler<DeviceStateSetCommand> deviceStateSerHandler,
            ICommandHandler<DeviceDiscoveredCommand> deviceDiscoveryHandler,
            IDevicesDao devicesDao,
            ILogger<PhilipsHueWorkerService> logger,
            IConfigurationService configurationService)
        {
            this.deviceStateSerHandler = deviceStateSerHandler ?? throw new ArgumentNullException(nameof(deviceStateSerHandler));
            this.deviceDiscoveryHandler = deviceDiscoveryHandler ?? throw new ArgumentNullException(nameof(deviceDiscoveryHandler));
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var configs = (await this.LoadBridgeConfigsAsync(cancellationToken)).ToList();
            if (!configs.Any())
                _ = this.DiscoverBridgesAsync(true, cancellationToken);
            else
            {
                // Connect to already configured bridges
                foreach (var bridgeConfig in configs)
                    _ = this.ConnectBridgeAsync(bridgeConfig, cancellationToken);
            }

            _ = Task.Run(() => this.PeriodicalLightStateRefreshAsync(cancellationToken), cancellationToken);
        }

        private async Task PeriodicalLightStateRefreshAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await this.RefreshDeviceStatesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to refresh devices state.");
                }

                await Task.Delay(5000, cancellationToken);
            }
        }

        private async Task RefreshDeviceStatesAsync(CancellationToken cancellationToken)
        {
            var bridge = await this.GetBridgeConnectionAsync();

            foreach (var (_, light) in this.lights)
                await this.RefreshLightStateAsync(bridge, light, cancellationToken);
        }

        private async Task<BridgeConnection> GetBridgeConnectionAsync()
        {
            var bridge = this.bridges.FirstOrDefault();
            if (bridge == null)
                throw new Exception("Bridge not initialized.");
            if (!await bridge.LocalClient.CheckConnection())
                throw new Exception("Bridge not connected.");
            
            return bridge;
        }

        private async Task RefreshLightStateAsync(BridgeConnection bridge, Light light, CancellationToken cancellationToken)
        {
            var updatedLight = await bridge.LocalClient.GetLightAsync(light.Id);
            if (updatedLight != null)
            {
                this.lights[light.UniqueId] = updatedLight;
                await this.deviceStateSerHandler.HandleAsync(
                    new DeviceStateSetCommand(new DeviceTarget(ToSignalDeviceId(light.UniqueId), "state"), updatedLight.State.On), 
                    cancellationToken);
            }
            else
            {
                this.logger.LogWarning(
                    "Light with ID {LightId} not found on bridge {BridgeName}.",
                    light.Id,
                    bridge.Config.Id);
            }
        }

        private async Task<IEnumerable<BridgeConfig>> LoadBridgeConfigsAsync(CancellationToken cancellationToken) => 
            await this.configurationService.LoadAsync<List<BridgeConfig>>("PhilipsHue", cancellationToken);

        private async Task SaveBridgeConfigsAsync(CancellationToken cancellationToken) => 
            await this.configurationService.SaveAsync("PhilipsHue", this.bridges.Select(b => b.Config), cancellationToken);

        private async Task ConnectBridgeAsync(BridgeConfig config, CancellationToken cancellationToken)
        {
            try
            {
                this.logger.LogInformation("Connecting to bridges {BridgeId} {BridgeIpAddress}...",
                    config.Id,
                    config.IpAddress);

                ILocalHueClient client = new LocalHueClient(config.IpAddress);

                var existingBridge = this.bridges.FirstOrDefault(b => b.Config.Id == config.Id);
                if (existingBridge != null)
                    existingBridge.LocalClient = client;
                else
                {
                    this.bridges.Add(new BridgeConnection
                    {
                        Config = config,
                        LocalClient = client
                    });
                }

                client.Initialize(config.LocalAppKey);
                if (!await client.CheckConnection())
                    throw new SocketException((int) SocketError.TimedOut);

                await this.SyncDevicesWithBridge(cancellationToken);
                await this.RefreshDeviceStatesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is SocketException {SocketErrorCode: SocketError.TimedOut} ||
                                       ex is HttpRequestException && ex.InnerException is SocketException
                                       {
                                           SocketErrorCode: SocketError.TimedOut
                                       })
            {
                this.logger.LogWarning(
                    "Bridge {BridgeIp} ({BridgeId}) didn't respond in time. Trying to rediscover on another IP address...",
                    config.IpAddress, config.Id);
                _ = this.DiscoverBridgesAsync(false, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to connect to bridge.");
            }
        }

        private async Task SyncDevicesWithBridge(CancellationToken cancellationToken)
        {
            try
            {
                var bridge = await this.GetBridgeConnectionAsync();
                var client = bridge.LocalClient;
                var lights = await client.GetLightsAsync();
                foreach (var light in lights)
                {
                    if (string.IsNullOrWhiteSpace(light.UniqueId))
                    {
                        this.logger.LogWarning("Device doesn't have unique ID.");
                        continue;
                    }

                    var existingDevice = await this.devicesDao.GetAsync(light.UniqueId, cancellationToken);
                    if (existingDevice == null)
                        this.NewLight(light, cancellationToken);
                    else throw new NotImplementedException("Updating existing device not supported yet.");

                    this.lights.Add(light.UniqueId, light);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to sync devices.");
            }
        }

        private void NewLight(Light light, CancellationToken cancellationToken)
        {
            var deviceConfig = new DeviceConfiguration(light.Name, ToSignalDeviceId(light.UniqueId))
            {
                Manufacturer = light.ManufacturerName,
                Model = light.ModelId,
                Endpoints = new[]
                {
                    new DeviceEndpoint("main",
                        new[] {new DeviceContact("state", "bool")},
                        new[] {new DeviceContact("state", "bool")})
                }
            };
            this.deviceDiscoveryHandler.HandleAsync(new DeviceDiscoveredCommand(deviceConfig), cancellationToken);
        }

        private static string ToPhilipsHueDeviceId(string signalId) => signalId.Substring(11);
        
        private static string ToSignalDeviceId(string uniqueId) => $"philipshue/{uniqueId}";

        private async Task DiscoverBridgesAsync(bool acceptNewBridges, CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Scanning for bridge...");
            var discoveredBridges = await HueBridgeDiscovery.CompleteDiscoveryAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
            this.logger.LogInformation("Bridges found: {BridgesCount}", discoveredBridges.Count);

            if (discoveredBridges.Count <= 0)
            {
                this.logger.LogInformation("No bridges found.");
                return;
            }

            var retryCounter = 0;
            var bridge = discoveredBridges.First();
            ILocalHueClient client = new LocalHueClient(bridge.IpAddress);
            while (retryCounter < RegisterBridgeRetryTimes &&
                   !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var existingId = this.bridges.FirstOrDefault(b => b.Config.Id == bridge.BridgeId);
                    if (existingId != null)
                    {
                        existingId.Config.IpAddress = bridge.IpAddress;
                        this.logger.LogInformation(
                            "Bridge rediscovered {BridgeIp} ({BridgeId}).",
                            existingId.Config.IpAddress, existingId.Config.Id);

                        _ = this.ConnectBridgeAsync(existingId.Config, cancellationToken);
                    }
                    else if (acceptNewBridges)
                    {
                        var appKey = await client.RegisterAsync("Signal.Beacon.Hue", "HueBeacon");
                        if (appKey == null)
                            throw new Exception("Hub responded with null key.");

                        this.logger.LogInformation("Bridge initializing...");

                        client.Initialize(appKey);

                        _ = this.ConnectBridgeAsync(new BridgeConfig
                        {
                            Id = bridge.BridgeId,
                            IpAddress = bridge.IpAddress,
                            LocalAppKey = appKey
                        }, cancellationToken);
                    }

                    break;
                }
                catch (LinkButtonNotPressedException ex)
                {
                    this.logger.LogTrace(ex, "Bridge not connected. Waiting for user button press.");
                    this.logger.LogInformation("Press button on Philips Hue bridge to connect...");
                    // TODO: Broadcast CTA on UI (ask user to press button on bridge)
                    retryCounter++;
                }
            }
        }
        
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await this.SaveBridgeConfigsAsync(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
        }
    }
}
