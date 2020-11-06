using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using Newtonsoft.Json;
using Signal.Beacon.Core.MessageQueue;

namespace Signal.Beacon.Application
{
    public class MqttClient : IMqttClient
    {
        private readonly ILogger<MqttClient> logger;
        private IManagedMqttClient? mqttClient;

        private readonly Dictionary<string, List<Func<MqttMessage, Task>>> subscriptions =
            new Dictionary<string, List<Func<MqttMessage, Task>>>();


        public MqttClient(ILogger<MqttClient> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.mqttClient != null)
                throw new Exception("Can't start client twice.");

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("Signal.Beacon")
                    .WithTcpServer("192.168.0.3") // TODO: Read from configuration
                    .Build())
                .Build();

            this.mqttClient = new MqttFactory().CreateManagedMqttClient();
            this.mqttClient.UseApplicationMessageReceivedHandler(this.MessageHandler);
            this.mqttClient.UseConnectedHandler(this.ConnectedHandler);
            this.mqttClient.UseDisconnectedHandler(this.DisconnectedHandler);
            await this.mqttClient.StartAsync(options);

            cancellationToken.Register(this.StopRequested);
        }

        public async Task SubscribeAsync(string topic, Func<MqttMessage, Task> handler)
        {
            await this.mqttClient.SubscribeAsync(
                new MqttTopicFilterBuilder().WithTopic(topic).Build());

            if (!this.subscriptions.ContainsKey(topic))
                this.subscriptions.Add(topic, new List<Func<MqttMessage, Task>>());
            this.subscriptions[topic].Add(handler);
        }

        public async Task PublishAsync(string topic, object? payload, bool retain = false)
        {
            await this.mqttClient.PublishAsync(
                new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(JsonConvert.SerializeObject(payload))
                    .WithRetainFlag(retain)
                    .Build());
        }

        private async Task MessageHandler(MqttApplicationMessageReceivedEventArgs arg)
        {
            var message = new MqttMessage(arg.ApplicationMessage.Topic, Encoding.ASCII.GetString(arg.ApplicationMessage.Payload), arg.ApplicationMessage.Payload);
            this.logger.LogTrace("Topic {Topic}, Payload: {Payload}", message.Topic, message.Payload);

            foreach (var subscription in this.subscriptions
                .Where(subscription => MqttTopicFilterComparer.IsMatch(arg.ApplicationMessage.Topic, subscription.Key))
                .SelectMany(s => s.Value))
            {
                try
                {
                    await subscription(message);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Queue subscriber threw exception while processing message.");
                }
            }
        }

        private Task DisconnectedHandler(MqttClientDisconnectedEventArgs arg)
        {
            this.logger.LogWarning(arg.Exception, "MQTT connection closed.");
            return Task.CompletedTask;
        }

        private Task ConnectedHandler(MqttClientConnectedEventArgs arg)
        {
            this.logger.LogInformation("MQTT connected.");
            return Task.CompletedTask;
        }

        private async void StopRequested()
        {
            if (this.mqttClient != null)
                await this.mqttClient.StopAsync();
        }
    }
}