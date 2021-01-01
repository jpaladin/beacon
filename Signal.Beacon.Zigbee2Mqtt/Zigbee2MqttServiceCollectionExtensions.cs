using Microsoft.Extensions.DependencyInjection;
using Signal.Beacon.Core.Workers;
using Signal.Beacon.Zigbee2Mqtt.MessageQueue;

namespace Signal.Beacon.Zigbee2Mqtt
{
    public static class Zigbee2MqttServiceCollectionExtensions
    {
        public static IServiceCollection AddZigbee2Mqtt(this IServiceCollection services)
        {
            return services
                .AddSingleton<IWorkerService, Zigbee2MqttWorkerService>()
                .AddSingleton<IZigbee2MqttClientFactory, Zigbee2MqttClientFactory>()
                .AddTransient<IMqttClient, MqttClient>();
        }
    }
}