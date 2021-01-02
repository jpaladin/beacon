using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Network;
using Signal.Beacon.Core.Workers;
using Signal.Beacon.Zigbee2Mqtt.MessageQueue;

namespace Signal.Beacon.Zigbee2Mqtt
{
    internal class Zigbee2MqttWorkerService : IWorkerService
    {
        private const string MqttTopicSubscription = "zigbee2mqtt/#";
        private const string ConfigurationFileName = "Zigbee2mqtt.json";
        private const int MqttClientStartRetryDelay = 10000;

        private readonly IDevicesDao devicesDao;
        private readonly ICommandHandler<DeviceStateSetCommand> deviceSetStateHandler;
        private readonly ICommandHandler<DeviceDiscoveredCommand> deviceDiscoverHandler;
        private readonly IConductSubscriberClient conductSubscriberClient;
        private readonly IZigbee2MqttClientFactory mqttClientFactory;
        private readonly IConfigurationService configurationService;
        private readonly IHostInfoService hostInfoService;
        private readonly ILogger<Zigbee2MqttWorkerService> logger;

        private readonly CancellationTokenSource cts = new();

        private readonly List<IMqttClient> clients = new();
        private Zigbee2MqttWorkerServiceConfiguration configuration = new();
        private CancellationToken startCancellationToken = CancellationToken.None;


        public Zigbee2MqttWorkerService(
            IDevicesDao devicesDao,
            ICommandHandler<DeviceStateSetCommand> devicesService,
            ICommandHandler<DeviceDiscoveredCommand> deviceDiscoverHandler,
            IConductSubscriberClient conductSubscriberClient,
            IZigbee2MqttClientFactory mqttClientFactory,
            IConfigurationService configurationService,
            IHostInfoService hostInfoService,
            ILogger<Zigbee2MqttWorkerService> logger)
        {
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
            this.deviceSetStateHandler = devicesService ?? throw new ArgumentNullException(nameof(devicesService));
            this.deviceDiscoverHandler = deviceDiscoverHandler ?? throw new ArgumentNullException(nameof(deviceDiscoverHandler));
            this.conductSubscriberClient = conductSubscriberClient ?? throw new ArgumentNullException(nameof(conductSubscriberClient));
            this.mqttClientFactory = mqttClientFactory ?? throw new ArgumentNullException(nameof(mqttClientFactory));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.hostInfoService = hostInfoService ?? throw new ArgumentNullException(nameof(hostInfoService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.startCancellationToken = cancellationToken;
            this.configuration =
                await this.configurationService.LoadAsync<Zigbee2MqttWorkerServiceConfiguration>(
                    ConfigurationFileName,
                    cancellationToken);

            if (this.configuration.Servers.Any())
                foreach (var mqttServerConfig in this.configuration.Servers.ToList())
                    this.StartMqttClient(mqttServerConfig);
            else
            {
                _ = this.DiscoverMqttBrokersAsync(cancellationToken);
            }

            this.conductSubscriberClient.Subscribe(Zigbee2MqttChannels.DeviceChannel, this.ConductHandler);
        }

        private async Task DiscoverMqttBrokersAsync(CancellationToken cancellationToken)
        {
            try
            {
                var ipAddressesInRange = IPHelper.GetIPAddressesInRange(IPHelper.GetLocalIp());
                var applicableHosts =
                    await this.hostInfoService.HostsAsync(ipAddressesInRange, new[] {1883}, cancellationToken);
                var brokerHost = applicableHosts.FirstOrDefault(h => h.OpenPorts.Any());
                if (brokerHost == null)
                {
                    this.logger.LogWarning("MQTT broker not configured and couldn't discover any.");
                    return;
                }

                try
                {
                    this.logger.LogInformation("Discovered possible MQTT broker on {IpAddress}. Connecting...",
                        brokerHost.IpAddress);

                    using var client = this.mqttClientFactory.Create();
                    await client.StartAsync(brokerHost.IpAddress, cancellationToken);
                    await client.StopAsync(cancellationToken);

                    // Save configuration for discovered broker
                    var config = new Zigbee2MqttWorkerServiceConfiguration.MqttServer
                    {
                        Url = brokerHost.IpAddress
                    };
                    this.configuration.Servers.Add(config);
                    await this.configurationService.SaveAsync(ConfigurationFileName, this.configuration,
                        cancellationToken);

                    // Connect to it
                    this.StartMqttClient(config);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(
                        ex,
                        "Failed to connect to discovered MQTT broker on {IpAddress}",
                        brokerHost.IpAddress);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "MQTT broker discovery failed.");
            }
        }

        private async void StartMqttClient(Zigbee2MqttWorkerServiceConfiguration.MqttServer mqttServerConfig)
        {
            try
            {
                var client = this.mqttClientFactory.Create();

                if (string.IsNullOrWhiteSpace(mqttServerConfig.Url))
                {
                    this.logger.LogWarning("MQTT Server has invalid URL: {Url}", mqttServerConfig.Url);
                    return;
                }

                await client.StartAsync(mqttServerConfig.Url, this.startCancellationToken);
                await client.SubscribeAsync(
                    MqttTopicSubscription,
                    m => this.MessageHandler(m, this.cts.Token));
                await client.PublishAsync("zigbee2mqtt/bridge/config/devices/get", null);
                await client.PublishAsync("zigbee2mqtt/bridge/config/permit_join", "false");

                this.clients.Add(client);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to start MQTT client on URL: {Url}. Retry in {MqttClientStartRetryDelay}ms", mqttServerConfig.Url, MqttClientStartRetryDelay);
                await Task
                    .Delay(MqttClientStartRetryDelay, this.startCancellationToken)
                    .ContinueWith(_ => this.StartMqttClient(mqttServerConfig), this.startCancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.cts.Cancel();
            await Task.WhenAll(this.clients.Select(c => c.StopAsync(cancellationToken)));
        }

        private async Task ConductHandler(Conduct conduct, CancellationToken cancellationToken)
        {
            if (conduct.Target == null)
            {
                this.logger.LogWarning("Conduct contact is null. Conduct: {@Conduct}", conduct);
                return;
            }

            await this.PublishStateAsync(
                conduct.Target.Identifier,
                conduct.Target.Contact,
                conduct.Value.ToString()?.ToLowerInvariant() == "true" ? "ON" : "OFF",
                cancellationToken);
        }

        private async Task MessageHandler(MqttMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var (topic, payload, _) = message;

                // Ignore logging
                if (topic.StartsWith("zigbee2mqtt/bridge/logging"))
                    return;
                
                if (topic == "zigbee2mqtt/bridge/devices")
                    await this.HandleDevicesConfigChangeAsync(message.Payload, cancellationToken);
                else await this.HandleDeviceTopicAsync(topic, payload, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to process message.");
            }
        }

        private async Task HandleDeviceTopicAsync(string topic, string payload, CancellationToken cancellationToken)
        {
            var device = await this.devicesDao.GetByAliasAsync(topic.Replace("zigbee2mqtt/", ""), cancellationToken);
            if (device == null)
            {
                this.logger.LogDebug("Device {DeviceIdentifier} not found", topic);
                return;
            }

            var inputs = device.Endpoints.SelectMany(e => e.Inputs).ToList();
            if (!inputs.Any())
            {
                this.logger.LogDebug("Device {DeviceIdentifier} has no inputs", topic);
                return;
            }

            foreach (var jProperty in JToken.Parse(payload)
                .Value<JObject>()
                .Properties())
            {
                var input = inputs.FirstOrDefault(i => i.Name == jProperty.Name);
                if (input == null)
                    continue;
                
                var target = new DeviceTarget(Zigbee2MqttChannels.DeviceChannel, device.Identifier, jProperty.Name);
                var value = jProperty.Value.Value<string>();
                var dataType = input.DataType;
                var mappedValue = MapZ2MValueToValue(dataType, value);

                try
                {
                    await this.deviceSetStateHandler.HandleAsync(new DeviceStateSetCommand(target, mappedValue), cancellationToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to set device state {Target} to {Value}.", target, value);
                }
            }
        }

        private async Task HandleDevicesConfigChangeAsync(string messagePayload, CancellationToken cancellationToken)
        {
            var config = JsonConvert.DeserializeObject<List<BridgeDevice>>(messagePayload);
            foreach (var bridgeDevice in config)
            {
                if (string.IsNullOrWhiteSpace(bridgeDevice.IeeeAddress))
                {
                    this.logger.LogWarning("Invalid IEEE address {IeeeAddress}. Device skipped.", bridgeDevice.IeeeAddress);
                    continue;
                }

                var existingDevice = await this.devicesDao.GetAsync(bridgeDevice.IeeeAddress, cancellationToken);
                if (existingDevice == null)
                    await this.NewDevice(bridgeDevice, cancellationToken);
                else await this.UpdateDevice(bridgeDevice);
            }
        }

        private async Task UpdateDevice(BridgeDevice bridgeDevice)
        {
            throw new NotImplementedException();
        }

        private async Task NewDevice(BridgeDevice bridgeDevice, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bridgeDevice.IeeeAddress))
                throw new ArgumentException("Device IEEE address is required.");

            var deviceConfig = new DeviceConfiguration(
                bridgeDevice.FriendlyName ?? bridgeDevice.IeeeAddress,
                $"zigbee2mqtt/{bridgeDevice.IeeeAddress}");

            if (bridgeDevice.Definition != null)
            {
                deviceConfig.Model = bridgeDevice.Definition.Model;
                deviceConfig.Manufacturer = bridgeDevice.Definition.Vendor;

                if (bridgeDevice.Definition.Exposes != null)
                {
                    var inputs = new List<DeviceContact>();
                    var outputs = new List<DeviceContact>();
                    foreach (var feature in bridgeDevice.Definition.Exposes.SelectMany(e =>
                        new List<BridgeDeviceExposeFeature>(e.Features ??
                                                            Enumerable.Empty<BridgeDeviceExposeFeature>()) {e}))
                    {
                        var name = feature.Property;
                        var type = feature.Type;

                        // Must have name and type
                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
                            continue;

                        // Map zigbee2mqtt type to signal data type
                        var dataType = MapZ2MTypeToDataType(type);
                        if (string.IsNullOrWhiteSpace(dataType))
                        {
                            this.logger.LogWarning(
                                "Failed to map input {Input} type {Type} for device {DeviceIdentifier}", 
                                name, type, deviceConfig.Identifier);
                            continue;
                        }
                        
                        var contact = new DeviceContact(name, dataType);
                        var isInput = feature.Access.HasFlag(BridgeDeviceExposeFeatureAccess.Readonly) ||
                                      feature.Access.HasFlag(BridgeDeviceExposeFeatureAccess.Request);
                        if (isInput)
                            inputs.Add(
                                contact with {
                                    IsReadonly = feature.Access.HasFlag(BridgeDeviceExposeFeatureAccess.Readonly)
                                    });
                        var isOutput = feature.Access.HasFlag(BridgeDeviceExposeFeatureAccess.Write);
                        if (isOutput) outputs.Add(contact);
                    }

                    if (inputs.Any() || outputs.Any())
                    {
                        deviceConfig.Endpoints = new List<DeviceEndpoint>
                        {
                            new(Zigbee2MqttChannels.DeviceChannel, inputs, outputs)
                        };
                    }
                }
            }

            await this.deviceDiscoverHandler.HandleAsync(new DeviceDiscoveredCommand(deviceConfig), cancellationToken);
            await this.RefreshDeviceAsync(deviceConfig);
        }

        private async Task RefreshDeviceAsync(DeviceConfiguration device)
        {
            try
            {
                var inputContacts =
                    device.Endpoints.SelectMany(e => e.Inputs.Where(i => !i.IsReadonly).Select(ei => ei.Name));

                // TODO: Publish only to specific client (that has device)

                var topic = $"zigbee2mqtt/{device.Alias}/get";
                var payload =
                    $"{{ {string.Join(", ", inputContacts.Select(inputContact => $"\"{inputContact}\": \"\""))} }}";

                await Task.WhenAll(this.clients.Select(c => c.PublishAsync(topic, payload)));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to publish message.");
            }
        }

        private async Task PublishStateAsync(string deviceIdentifier, string contactName, string value, CancellationToken cancellationToken)
        {
            try
            {
                var device = await this.devicesDao.GetAsync(deviceIdentifier, cancellationToken);
                if (device == null)
                    throw new Exception($"Device with identifier {deviceIdentifier} not found.");

                // TODO: Publish only to specific client (that has device)

                var topic = $"zigbee2mqtt/{device.Alias}/set/{contactName}";
                await Task.WhenAll(this.clients.Select(c => c.PublishAsync(topic, value)));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to publish message.");
            }
        }

        private static object? MapZ2MValueToValue(string dataType, string? value)
        {
            return dataType switch
            {
                "bool" => ValueToBool(value),
                "double" => ValueToNumeric(value),
                "string" => value,
                _ => value
            };
        }

        private static object? ValueToNumeric(string? value) => 
            double.TryParse(value, out var doubleValue) ? doubleValue : value;

        private static object? ValueToBool(string? value) =>
            bool.TryParse(value, out var boolVal)
                ? boolVal
                : value?.ToLowerInvariant() switch
                {
                    "on" => true,
                    "off" => false,
                    _ => value
                };

        private static string? MapZ2MTypeToDataType(string type) =>
            type switch
            {
                "binary" => "bool",
                "numeric" => "double",
                "enum" => "string",
                _ => null
            };
    }
}