using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.MessageQueue;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.Zigbee2Mqtt
{
    internal class Zigbee2MqttWorkerService : IWorkerService
    {
        private const string MqttTopicSubscription = "zigbee2mqtt/#";

        private readonly IDevicesDao devicesDao;
        private readonly ICommandHandler<DeviceStateSetCommand> deviceSetStateHandler;
        private readonly ICommandHandler<DeviceDiscoveredCommand> deviceDiscoverHandler;
        private readonly IMqttClient mqttClient;
        private readonly ILogger<Zigbee2MqttWorkerService> logger;


        public Zigbee2MqttWorkerService(
            IDevicesDao devicesDao,
            ICommandHandler<DeviceStateSetCommand> devicesService,
            ICommandHandler<DeviceDiscoveredCommand> deviceDiscoverHandler,
            IMqttClient mqttClient,
            ILogger<Zigbee2MqttWorkerService> logger)
        {
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
            this.deviceSetStateHandler = devicesService ?? throw new ArgumentNullException(nameof(devicesService));
            this.deviceDiscoverHandler = deviceDiscoverHandler;
            this.mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Starting Zigbee2Mqtt...");

            await this.mqttClient.SubscribeAsync(MqttTopicSubscription, this.MessageHandler);
            await this.mqttClient.SubscribeAsync("signal/conducts/zigbee2mqtt", this.ConductHandler);

            await this.mqttClient.PublishAsync("zigbee2mqtt/bridge/config/devices/get", null);
        }

        private async Task ConductHandler(MqttMessage arg)
        {
            var conduct = JsonConvert.DeserializeObject<Conduct>(arg.Payload);
            await this.PublishStateAsync(conduct.Target.Identifier, conduct.Target.Contact, conduct.Value?.ToString());
        }


        private async Task MessageHandler(MqttMessage message)
        {
            try
            {
                var (topic, payload, _) = message;

                if (topic == "zigbee2mqtt/bridge/devices")
                    await this.HandleDevicesConfigChangeAsync(message.Payload);
                else await this.HandleDeviceTopicAsync(topic, payload);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to process message.");
            }
        }

        private async Task HandleDeviceTopicAsync(string topic, string payload)
        {
            var device = await this.devicesDao.GetByAliasAsync(topic.Replace("zigbee2mqtt/", ""));
            if (device == null)
            {
                this.logger.LogDebug("Device {DeviceIdentifier} not found", topic);
                return;
            }

            var deviceTarget = new DeviceTarget(device.Identifier);
            var inputs = device.Endpoints.SelectMany(e => e.Inputs.Select(ei => ei.Name)).ToList();
            if (!inputs.Any())
            {
                this.logger.LogDebug("Device {DeviceIdentifier} has no inputs", topic);
                return;
            }

            foreach (var jProperty in JToken.Parse(payload)
                .Value<JObject>()
                .Properties()
                .Where(jp => inputs.Contains(jp.Name)))
            {
                var target = deviceTarget with {Contact = jProperty.Name};
                var value = jProperty.Value.Value<string>();

                try
                {
                    await this.deviceSetStateHandler.HandleAsync(new DeviceStateSetCommand(target, value));
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to set device state {Target} to {Value}.", target, value);
                }
            }
        }

        private async Task HandleDevicesConfigChangeAsync(string messagePayload)
        {
            var config = JsonConvert.DeserializeObject<List<BridgeDevice>>(messagePayload);
            foreach (var bridgeDevice in config)
            {
                if (string.IsNullOrWhiteSpace(bridgeDevice.IeeeAddress))
                    continue;
                
                var existingDevice = await this.devicesDao.GetAsync(bridgeDevice.IeeeAddress);
                if (existingDevice == null)
                {
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
                                var isInput = feature.Access?.Contains("r") ?? false;
                                var isOutput = feature.Access?.Contains("w") ?? false;
                                var name = feature.Property;
                                var type = feature.Type;

                                // Must have name and type
                                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
                                    continue;

                                var contact = new DeviceContact(name, MapToDataType(type));
                                if (isInput)
                                    inputs.Add(contact);
                                if (isOutput)
                                    outputs.Add(contact);
                            }

                            if (inputs.Any() || outputs.Any())
                            {
                                deviceConfig.Endpoints = new List<DeviceEndpoint>
                                {
                                    new DeviceEndpoint("main", inputs, outputs)
                                };
                            }
                        }
                    }

                    await this.deviceDiscoverHandler.HandleAsync(new DeviceDiscoveredCommand(deviceConfig));
                }
            }
        }

        private static string? MapToDataType(string type) =>
            type switch
            {
                "binary" => "bool",
                "numeric" => "double",
                _ => null
            };

        private async Task PublishStateAsync(string deviceIdentifier, string contactName, string value)
        {
            try
            {
                var device = await this.devicesDao.GetAsync(deviceIdentifier);
                if (device == null)
                    throw new Exception($"Device with identifier {deviceIdentifier} not found.");

                await this.mqttClient.PublishAsync($"zigbee2mqtt/{device.Alias}/set/{contactName}", value);
            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, "Failed to publish message.");
            }
        }
    }
    
    internal class BridgeDevice
    {
        [JsonProperty("ieee_address", NullValueHandling = NullValueHandling.Ignore)]
        public string? IeeeAddress { get; set; }

        [JsonProperty("friendly_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? FriendlyName { get; set;  }

        public BridgeDeviceDefinition? Definition { get; set; }
    }

    internal class BridgeDeviceDefinition
    {
        public string? Model { get; set; }

        public string? Vendor { get; set; }

        public List<BridgeDeviceExposeFeature>? Exposes { get; set; }
    }
    
    internal class BridgeDeviceExposeFeature
    {
        public string? Access { get; set; }

        public string? Property { get; set; }

        public string? Type { get; set; }

        public List<BridgeDeviceExposeFeature>? Features { get; set; }
    }
}