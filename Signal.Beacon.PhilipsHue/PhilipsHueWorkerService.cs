using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.MessageQueue;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.PhilipsHue
{
    public class BridgeConfig
    {
        public string Id { get; init; }

        public string IpAddress { get; init; }

        public string LocalAppKey { get; init; }
    }

    public class BridgeConnection
    {
        public BridgeConfig Config { get; init; }

        public ILocalHueClient LocalClient { get; init; }
    }

    public class PhilipsHueWorkerService : IWorkerService, IDisposable
    {
        private readonly IMqttClient mqttClient;
        private readonly ICommandHandler<DeviceStateSetCommand> deviceStateSerHandler;
        private readonly ICommandHandler<DeviceDiscoveredCommand> deviceDiscoveryHandler;
        private readonly IDevicesDao devicesDao;
        private readonly ILogger<PhilipsHueWorkerService> logger;
        private readonly IConfigurationService configurationService;

        private const int RegisterBridgeRetryTimes = 5;

        private readonly List<BridgeConnection> bridges = new();
        private readonly Dictionary<string, Light> lights = new();

        public PhilipsHueWorkerService(
            IMqttClient mqttClient,
            ICommandHandler<DeviceStateSetCommand> deviceStateSerHandler,
            ICommandHandler<DeviceDiscoveredCommand> deviceDiscoveryHandler,
            IDevicesDao devicesDao,
            ILogger<PhilipsHueWorkerService> logger,
            IConfigurationService configurationService)
        {
            this.mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            this.deviceStateSerHandler = deviceStateSerHandler ?? throw new ArgumentNullException(nameof(deviceStateSerHandler));
            this.deviceDiscoveryHandler = deviceDiscoveryHandler ?? throw new ArgumentNullException(nameof(deviceDiscoveryHandler));
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var configs = (await this.LoadBridgeConfigsAsync()).ToList();
            if (!configs.Any())
                _ = this.RegisterNewBridgeAsync(cancellationToken);
            else
            {
                // Connect to already configured bridges
                foreach (var bridgeConfig in configs)
                    _ = this.ConnectBridgeAsync(bridgeConfig);
            }

            await this.mqttClient.SubscribeAsync("signal/conducts/#", this.ConductHandler);

            _ = Task.Run(() => this.PeriodicalLightStateRefreshAsync(cancellationToken), cancellationToken);
        }

        private async Task PeriodicalLightStateRefreshAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                await this.RefreshDeviceStatesAsync();
                await Task.Delay(5000, cancellationToken);
            }
        }

        private async Task ConductHandler(MqttMessage arg)
        {
            var bridge = this.bridges.FirstOrDefault();
            if (bridge == null)
                throw new Exception("Bridge not initialized.");

            // TODO: Handle multiple bridges somehow
            if (!await bridge.LocalClient.CheckConnection())
                throw new Exception("Bridge not connected.");

            var conduct = JsonConvert.DeserializeObject<Conduct>(arg.Payload);

            if (this.lights.TryGetValue(conduct.Target.Identifier, out var light))
            {
                await bridge.LocalClient.SendCommandAsync(new LightCommand
                {
                    On = conduct.Value.ToString()?.ToLowerInvariant() == "true"
                }, new[] {light.Id});

                await this.RefreshLightStateAsync(bridge, light);
            }
        }

        private async Task RefreshDeviceStatesAsync()
        {
            var bridge = this.bridges.FirstOrDefault();
            if (bridge == null)
                throw new Exception("Bridge not initialized.");
            if (!await bridge.LocalClient.CheckConnection())
                throw new Exception("Bridge not connected.");

            foreach (var (_, light) in this.lights)
                await this.RefreshLightStateAsync(bridge, light);
        }

        private async Task RefreshLightStateAsync(BridgeConnection bridge, Light light)
        {
            var updatedLight = await bridge.LocalClient.GetLightAsync(light.Id);
            if (updatedLight != null)
            {
                this.lights[light.UniqueId] = updatedLight;
                await this.deviceStateSerHandler.HandleAsync(
                    new DeviceStateSetCommand(new DeviceTarget(light.UniqueId, "state"), updatedLight.State.On));
            }
            else
            {
                this.logger.LogWarning(
                    "Light with ID {LightId} not found on bridge {BridgeName}.",
                    light.Id,
                    bridge.Config.Id);
            }
        }

        private async Task<IEnumerable<BridgeConfig>> LoadBridgeConfigsAsync() => 
            await this.configurationService.LoadAsync<List<BridgeConfig>>("PhilipsHue");

        private async Task SaveBridgeConfigsAsync() => 
            await this.configurationService.SaveAsync("PhilipsHue", this.bridges.Select(b => b.Config));

        private async Task ConnectBridgeAsync(BridgeConfig config)
        {
            try
            {
                this.logger.LogInformation("Connecting to bridges {BridgeId} {BridgeIpAddress}...", config.Id,
                    config.IpAddress);

                ILocalHueClient client = new LocalHueClient(config.IpAddress);
                client.Initialize(config.LocalAppKey);

                this.bridges.Add(new BridgeConnection
                {
                    Config = config,
                    LocalClient = client
                });

                var lights = await client.GetLightsAsync();
                foreach (var light in lights)
                {
                    if (string.IsNullOrWhiteSpace(light.UniqueId))
                    {
                        this.logger.LogWarning("Device doesn't have unique ID.");
                        continue;
                    }

                    var existingDevice = await this.devicesDao.GetAsync(light.UniqueId);
                    if (existingDevice == null)
                        this.NewLight(light);
                    else throw new NotImplementedException("Updating existing device not supported yet.");

                    this.lights.Add(light.UniqueId, light);
                }

                await this.RefreshDeviceStatesAsync();
            }
            catch(Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to connect to bridge.");
            }
        }

        private void NewLight(Light light)
        {
            var deviceConfig = new DeviceConfiguration(light.Name, light.UniqueId)
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
            this.deviceDiscoveryHandler.HandleAsync(new DeviceDiscoveredCommand(deviceConfig));
        }

        private async Task RegisterNewBridgeAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Scanning for bridge...");
            var discoveredBridges = await HueBridgeDiscovery.CompleteDiscoveryAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
            this.logger.LogInformation("Bridges found: {BridgesCount}", discoveredBridges.Count);

            if (discoveredBridges.Count > 0)
            {
                var retryCounter = 0;
                var bridge = discoveredBridges.First();
                ILocalHueClient client = new LocalHueClient(bridge.IpAddress);
                while (retryCounter < RegisterBridgeRetryTimes && 
                       !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(5000, cancellationToken);
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
                        });

                        break;
                    }
                    catch (LinkButtonNotPressedException ex)
                    {
                        this.logger.LogTrace(ex, "Bridge not connected. Waiting for user button press.");
                        this.logger.LogInformation("Press button on Philips Hue bridge to connect...");
                        // TODO: Broadcast CTA
                        retryCounter++;
                    }
                }
            }

            this.logger.LogInformation("No bridges found.");
        }

        public void Dispose()
        {
            _ = this.SaveBridgeConfigsAsync();
        }
    }
}
