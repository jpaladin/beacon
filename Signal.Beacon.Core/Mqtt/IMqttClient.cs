using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Core.Mqtt
{
    public interface IMqttClient : IDisposable
    {
        Task StartAsync(string clientName, string hostAddress, CancellationToken cancellationToken);
        
        Task StopAsync(CancellationToken cancellationToken);

        Task SubscribeAsync(string topic, Func<MqttMessage, Task> handler);

        Task PublishAsync(string topic, object? payload, bool retain = false);
    }
}