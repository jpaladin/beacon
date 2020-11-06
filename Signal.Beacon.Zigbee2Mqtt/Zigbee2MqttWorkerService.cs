using System;
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

        private readonly IDevicesService devicesService;
        private readonly IMqttClient mqttClient;
        private readonly ILogger<Zigbee2MqttWorkerService> logger;


        public Zigbee2MqttWorkerService(
            IDevicesService devicesService,
            IMqttClient mqttClient,
            ILogger<Zigbee2MqttWorkerService> logger)
        {
            this.devicesService = devicesService ?? throw new ArgumentNullException(nameof(devicesService));
            this.mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Starting Zigbee2Mqtt...");

            await this.mqttClient.SubscribeAsync(MqttTopicSubscription, MessageHandler);
            await this.mqttClient.SubscribeAsync("signal/conducts/zigbee2mqtt", ConductHandler);
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
                var (deviceIdentifier, payload, _) = message;

                var device = await this.devicesService.GetAsync(deviceIdentifier);
                if (device == null)
                {
                    this.logger.LogDebug("Device {DeviceIdentifier} not found", deviceIdentifier);
                    return;
                }

                var deviceTarget = new DeviceTarget(deviceIdentifier);
                var inputs = device.Endpoints.SelectMany(e => e.Inputs.Select(ei => ei.Name)).ToList();
                if (!inputs.Any())
                {
                    this.logger.LogDebug("Device {DeviceIdentifier} has no inputs", deviceIdentifier);
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
                        this.devicesService.SetState(target, value);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "Failed to set device state {Target} to {Value}.", target, value);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to process message.");
            }
        }

        private async Task PublishStateAsync(string device, string contactName, string value)
        {
            try
            {
                await this.mqttClient.PublishAsync($"{device}/set/{contactName}", value);
            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, "Failed to publish message.");
            }
        }
    }
}