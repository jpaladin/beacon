using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Core.MessageQueue
{
    public interface IMqttClient
    {
        Task StartAsync(CancellationToken cancellationToken);

        Task SubscribeAsync(string topic, Func<MqttMessage, Task> handler);

        Task PublishAsync(string topic, object? payload, bool retain = false);
    }
}