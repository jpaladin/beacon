using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Core.Conducts
{
    public interface IConductService
    {
        Task PublishConductsAsync(IEnumerable<Conduct> conduct, CancellationToken cancellationToken);
    }
}