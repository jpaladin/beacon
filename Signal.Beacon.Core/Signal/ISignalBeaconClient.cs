using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Core.Signal
{
    public interface ISignalBeaconClient
    {
        Task RegisterBeaconAsync(
            string beaconId,
            CancellationToken cancellationToken);
    }
}