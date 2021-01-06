using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using Signal.Beacon.Core.Mqtt;
using IMqttClient = Signal.Beacon.Core.Mqtt.IMqttClient;

namespace Signal.Beacon.Application.Mqtt
{
    public class MqttClient : IMqttClient
    {
        private readonly ILogger<MqttClient> logger;
        private IManagedMqttClient? mqttClient;

        private readonly Dictionary<string, List<Func<MqttMessage, Task>>> subscriptions = new();


        public MqttClient(ILogger<MqttClient> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task StartAsync(string clientName, string hostAddress, CancellationToken cancellationToken)
        {
            if (this.mqttClient != null)
                throw new Exception("Can't start client twice.");
                        
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(hostAddress);
                var selectedAddress = addresses.FirstOrDefault();
                if (selectedAddress == null)
                    throw new Exception("Invalid host address - none.");

                var options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(new MqttClientOptionsBuilder()
                        .WithClientId(clientName)
                        .WithTcpServer(selectedAddress.ToString())
                        .Build())
                    .Build();

                this.mqttClient = new MqttFactory().CreateManagedMqttClient();
                this.mqttClient.UseApplicationMessageReceivedHandler(this.MessageHandler);
                this.mqttClient.UseConnectedHandler(this.ConnectedHandler);
                this.mqttClient.UseDisconnectedHandler(this.DisconnectedHandler);
                await this.mqttClient.StartAsync(options);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
            {
                this.logger.LogError("Unable to resolve MQTT host: {HostAddress}", hostAddress);
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to start MQTT client.");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.mqttClient != null)
                await this.mqttClient.StopAsync();
        }

        public async Task SubscribeAsync(string topic, Func<MqttMessage, Task> handler)
        {
            await this.mqttClient.SubscribeAsync(
                new MqttTopicFilterBuilder().WithTopic(topic).Build());

            if (!this.subscriptions.ContainsKey(topic))
            {
                this.subscriptions.Add(topic, new List<Func<MqttMessage, Task>>());
                this.logger.LogDebug("Subscribed to topic: {Topic}", topic);
            }

            this.subscriptions[topic].Add(handler);
        }

        public async Task PublishAsync(string topic, object? payload, bool retain = false)
        {
            var withPayload = payload == null
                ? null
                : payload is string payloadString
                    ? payloadString
                    : JsonSerializer.Serialize(payload, payload.GetType());

            await this.mqttClient.PublishAsync(
                new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(withPayload)
                    .WithRetainFlag(retain)
                    .Build());
        }

        private async Task MessageHandler(MqttApplicationMessageReceivedEventArgs arg)
        {
            var message = new MqttMessage(this, arg.ApplicationMessage.Topic, Encoding.ASCII.GetString(arg.ApplicationMessage.Payload), arg.ApplicationMessage.Payload);
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
            this.logger.LogInformation(arg.Exception, "MQTT connection closed.");
            return Task.CompletedTask;
        }

        private Task ConnectedHandler(MqttClientConnectedEventArgs arg)
        {
            this.logger.LogInformation("MQTT connected.");
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {
            this.mqttClient?.Dispose();
        }
    }
}