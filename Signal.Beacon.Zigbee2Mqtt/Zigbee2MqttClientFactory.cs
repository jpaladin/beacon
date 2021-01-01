using System;
using Microsoft.Extensions.DependencyInjection;
using Signal.Beacon.Zigbee2Mqtt.MessageQueue;

namespace Signal.Beacon.Zigbee2Mqtt
{
    internal class Zigbee2MqttClientFactory : IZigbee2MqttClientFactory
    {
        private readonly IServiceProvider serviceProvider;

        public Zigbee2MqttClientFactory(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IMqttClient Create() => this.serviceProvider.GetRequiredService<IMqttClient>();
    }
}