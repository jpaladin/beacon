using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Application.Mqtt;
using Signal.Beacon.Core.Architecture;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Mqtt;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.Channel.Tasmota
{
    public class TasmotaWorkerService : IWorkerService
    {
        private const string ConfigurationFileName = "Tasmota.json";

        private readonly IMqttClientFactory mqttClientFactory;
        private readonly IMqttDiscoveryService mqttDiscoveryService;
        private readonly IConfigurationService configurationService;
        private readonly IConductSubscriberClient conductSubscriberClient;
        private readonly ICommandHandler<DeviceDiscoveredCommand> deviceDiscoveryHandler;
        private readonly ICommandHandler<DeviceStateSetCommand> deviceStateHandler;
        private readonly ILogger<TasmotaWorkerService> logger;
        private readonly List<IMqttClient> clients = new();

        private TasmotaWorkerServiceConfiguration configuration = new();
        private CancellationToken startCancellationToken;

        public TasmotaWorkerService(
            IMqttClientFactory mqttClientFactory,
            IMqttDiscoveryService mqttDiscoveryService,
            IConfigurationService configurationService,
            IConductSubscriberClient conductSubscriberClient,
            ICommandHandler<DeviceDiscoveredCommand> deviceDiscoveryHandler,
            ICommandHandler<DeviceStateSetCommand> deviceStateHandler,
            ILogger<TasmotaWorkerService> logger)
        {
            this.mqttClientFactory = mqttClientFactory ?? throw new ArgumentNullException(nameof(mqttClientFactory));
            this.mqttDiscoveryService = mqttDiscoveryService ?? throw new ArgumentNullException(nameof(mqttDiscoveryService));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.conductSubscriberClient = conductSubscriberClient ?? throw new ArgumentNullException(nameof(conductSubscriberClient));
            this.deviceDiscoveryHandler = deviceDiscoveryHandler ?? throw new ArgumentNullException(nameof(deviceDiscoveryHandler));
            this.deviceStateHandler = deviceStateHandler ?? throw new ArgumentNullException(nameof(deviceStateHandler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.startCancellationToken = cancellationToken;
            this.configuration =
                await this.configurationService.LoadAsync<TasmotaWorkerServiceConfiguration>(
                    ConfigurationFileName,
                    cancellationToken);

            if (this.configuration.Servers.Any())
                foreach (var mqttServerConfig in this.configuration.Servers.ToList())
                    this.StartMqttClientAsync(mqttServerConfig);
            else
            {
                this.DiscoverMqttBrokersAsync(cancellationToken);
            }

            this.conductSubscriberClient.Subscribe(TasmotaChannels.DeviceChannel, this.ConductHandler);
        }

        private async Task ConductHandler(Conduct conduct, CancellationToken cancellationToken)
        {
            var client = this.clients.FirstOrDefault();
            if (client != null)
                await client.PublishAsync($"signal/{conduct.Target.Identifier}/{conduct.Target.Contact}/set", conduct.Value);
        }

        private async void StartMqttClientAsync(TasmotaWorkerServiceConfiguration.MqttServer mqttServerConfig)
        {
            var client = this.mqttClientFactory.Create();
            await client.StartAsync("Signal.Beacon.Channel.Tasmota", mqttServerConfig.Url, this.startCancellationToken);
            await client.SubscribeAsync("tasmota/discovery/#", this.DiscoverDevicesAsync);
            this.clients.Add(client);
        }

        private async Task DiscoverDevicesAsync(MqttMessage message)
        {
            var config = JsonSerializer.Deserialize<TasmotaConfig>(message.Payload);

            var discoveryType = message.Topic.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
            if (discoveryType == "config")
            {
                // Signal new device discovered
                await this.deviceDiscoveryHandler.HandleAsync(
                    new DeviceDiscoveredCommand(
                        config.DeviceName,
                        $"{TasmotaChannels.DeviceChannel}/{config.Topic}",
                        new DeviceEndpoint[]
                        {
                            new(TasmotaChannels.DeviceChannel,
                                new[] {new DeviceContact("A0", "double") {NoiseReductionDelta = 5}})
                        }),
                    this.startCancellationToken);

                // Subscribe for device telemetry
                var telemetrySubscribeTopic = config.FullTopic
                    .Replace("%topic%", config.Topic)
                    .Replace("%prefix%", config.TopicPrefixes[2]);
                await message.Client.SubscribeAsync($"{telemetrySubscribeTopic}#",
                    msg => this.TelemetryHandlerAsync($"{TasmotaChannels.DeviceChannel}/{config.Topic}", msg));
            }
            else if (discoveryType == "sensors")
            {
                // TODO: Assign endpoints
            }
        }

        private async Task TelemetryHandlerAsync(string deviceIdentifier, MqttMessage message)
        {
            var type = message.Topic.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
            if (type == "SENSOR")
            {
                var telemetry = JsonSerializer.Deserialize<TasmotaSensorTelemetry>(message.Payload);
                if (telemetry?.Analog?.A0 != null)
                {
                    var target = new DeviceTarget(TasmotaChannels.DeviceChannel, deviceIdentifier, "A0");
                    var newValue = telemetry.Analog.A0;
                    await this.deviceStateHandler.HandleAsync(
                        new DeviceStateSetCommand(target, newValue),
                        this.startCancellationToken);
                }
            }
            else if (type == "LWT")
            {
                // TODO: Handle Online/Offline
            }
        }

        private async void DiscoverMqttBrokersAsync(CancellationToken cancellationToken)
        {
            var availableBrokers =
                await this.mqttDiscoveryService.DiscoverMqttBrokerHostsAsync("tasmota/#", cancellationToken);
            foreach (var availableBroker in availableBrokers)
            {
                // TODO: Check for duplicates
                var config = new TasmotaWorkerServiceConfiguration.MqttServer(availableBroker.IpAddress);
                this.configuration.Servers.Add(config);
                await this.configurationService.SaveAsync(ConfigurationFileName, this.configuration, cancellationToken);
                this.StartMqttClientAsync(config);
            }
        }
        
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var mqttClient in this.clients) 
                await mqttClient.StopAsync(cancellationToken);
        }
    }
}
