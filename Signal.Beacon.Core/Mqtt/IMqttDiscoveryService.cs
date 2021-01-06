using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Network;

namespace Signal.Beacon.Application.Mqtt
{
    public interface IMqttDiscoveryService
    {
        Task<IEnumerable<IHostInfo>> DiscoverMqttBrokerHostsAsync(
            string expectedTopic,
            CancellationToken cancellationToken);
    }
}