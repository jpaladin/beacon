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

namespace Signal.Beacon.Channel.Signal
{
    public class SignalWorkerService : IWorkerService
    {
        private const string ConfigurationFileName = "Signal.json";

        private readonly IDevicesDao devicesDao;
        private readonly IMqttClientFactory mqttClientFactory;
        private readonly IMqttDiscoveryService mqttDiscoveryService;
        private readonly IConfigurationService configurationService;
        private readonly IConductSubscriberClient conductSubscriberClient;
        private readonly ICommandHandler<DeviceDiscoveredCommand> deviceDiscoveryHandler;
        private readonly ICommandHandler<DeviceStateSetCommand> deviceStateHandler;
        private readonly ILogger<SignalWorkerService> logger;
        private readonly List<IMqttClient> clients = new();

        private SignalWorkerServiceConfiguration configuration = new();
        private CancellationToken startCancellationToken;

        public SignalWorkerService(
            IDevicesDao devicesDao,
            IMqttClientFactory mqttClientFactory,
            IMqttDiscoveryService mqttDiscoveryService,
            IConfigurationService configurationService,
            IConductSubscriberClient conductSubscriberClient,
            ICommandHandler<DeviceDiscoveredCommand> deviceDiscoveryHandler,
            ICommandHandler<DeviceStateSetCommand> deviceStateHandler,
            ILogger<SignalWorkerService> logger)
        {
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
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
                await this.configurationService.LoadAsync<SignalWorkerServiceConfiguration>(
                    ConfigurationFileName,
                    cancellationToken);

            if (this.configuration.Servers.Any())
                foreach (var mqttServerConfig in this.configuration.Servers.ToList())
                    this.StartMqttClientAsync(mqttServerConfig);
            else
            {
                this.DiscoverMqttBrokersAsync(cancellationToken);
            }

            this.conductSubscriberClient.Subscribe(SignalChannels.DeviceChannel, this.ConductHandler);
        }

        private async Task ConductHandler(Conduct conduct, CancellationToken cancellationToken)
        {
            var localIdentifier = conduct.Target.Identifier[7..];
            var client = this.clients.FirstOrDefault();
            if (client != null)
                await client.PublishAsync($"{conduct.Target.Channel}/{localIdentifier}/{conduct.Target.Contact}/set", conduct.Value);
        }

        private async void StartMqttClientAsync(SignalWorkerServiceConfiguration.MqttServer mqttServerConfig)
        {
            var client = this.mqttClientFactory.Create();
            await client.StartAsync("Signal.Beacon.Channel.Signal", mqttServerConfig.Url, this.startCancellationToken);
            await client.SubscribeAsync("signal/discovery/#", this.DiscoverDevicesAsync);
            this.clients.Add(client);
        }

        private async Task DiscoverDevicesAsync(MqttMessage message)
        {
            var config = JsonSerializer.Deserialize<SignalDeviceConfig>(message.Payload);

            var discoveryType = message.Topic.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
            if (discoveryType == "config")
            {
                var deviceIdentifier = $"{SignalChannels.DeviceChannel}/{config.MqttTopic}";

                try
                {
                    // Signal new device discovered
                    await this.deviceDiscoveryHandler.HandleAsync(
                        new DeviceDiscoveredCommand(
                            config.Hostname,
                            deviceIdentifier,
                            new DeviceEndpoint[]
                            {
                                // TODO: Parse endpoint configuration
                                new(SignalChannels.DeviceChannel,
                                    new[]
                                    {
                                        new DeviceContact("locked", "bool", DeviceContactAccess.Read),
                                        new DeviceContact("lock", "bool", DeviceContactAccess.Write),
                                        new DeviceContact("unlock", "bool", DeviceContactAccess.Write),
                                        new DeviceContact("open", "bool", DeviceContactAccess.Write)
                                    }),
                            }),
                        this.startCancellationToken);

                    // Subscribe for device telemetry
                    var telemetrySubscribeTopic = $"signal/{config.MqttTopic}/#";
                    await message.Client.SubscribeAsync(telemetrySubscribeTopic,
                        msg => this.TelemetryHandlerAsync($"{SignalChannels.DeviceChannel}/{config.MqttTopic}",
                            msg));
                }
                catch (Exception ex)
                {
                    this.logger.LogTrace(ex, "Failed to configure device {Name} ({Identifier})", config.Hostname,
                        deviceIdentifier);
                    this.logger.LogWarning("Failed to configure device {Name} ({Identifier})", config.Hostname,
                        deviceIdentifier);
                }

                // Publish telemetry refresh request
                await message.Client.PublishAsync($"signal/{config.MqttTopic}/get", "get");
            }
        }

        private async Task TelemetryHandlerAsync(string deviceIdentifier, MqttMessage message)
        {
            var isTelemetry = deviceIdentifier == message.Topic;
            if (isTelemetry)
            {
                var telemetry = JsonSerializer.Deserialize<SignalSensorTelemetry>(message.Payload);
                if (telemetry?.Locked != null)
                    await this.deviceStateHandler.HandleAsync(new DeviceStateSetCommand(
                            new DeviceTarget(SignalChannels.DeviceChannel, deviceIdentifier, "locked"),
                            telemetry.Locked),
                        this.startCancellationToken);
            }
        }

        private async void DiscoverMqttBrokersAsync(CancellationToken cancellationToken)
        {
            var availableBrokers =
                await this.mqttDiscoveryService.DiscoverMqttBrokerHostsAsync("signal/#", cancellationToken);
            foreach (var availableBroker in availableBrokers)
            {
                this.configuration.Servers.Add(new SignalWorkerServiceConfiguration.MqttServer
                    { Url = availableBroker.IpAddress });
                await this.configurationService.SaveAsync(ConfigurationFileName, this.configuration, cancellationToken);
                this.StartMqttClientAsync(
                    new SignalWorkerServiceConfiguration.MqttServer
                        {Url = availableBroker.IpAddress});
            }
        }
        
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var mqttClient in this.clients) 
                await mqttClient.StopAsync(cancellationToken);
        }
    }
}