using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Zigbee2Mqtt.MessageQueue
{
    public interface IMqttClient : IDisposable
    {
        Task StartAsync(string hostAddress, CancellationToken cancellationToken);
        
        Task StopAsync(CancellationToken cancellationToken);

        Task SubscribeAsync(string topic, Func<MqttMessage, Task> handler);

        Task PublishAsync(string topic, object? payload, bool retain = false);
    }
}