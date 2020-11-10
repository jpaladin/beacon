using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signal.Beacon.Core.Conducts
{
    public interface IConductService
    {
        Task PublishConductsAsync(string wireIdentifier, IEnumerable<Conduct> conduct);
    }
}