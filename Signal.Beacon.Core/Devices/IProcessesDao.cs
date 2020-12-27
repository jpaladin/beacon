using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Core.Devices
{
    public interface IProcessesDao
    {
        Task<IEnumerable<Process>> GetAllAsync(CancellationToken cancellationToken);
    }
}