using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Core.Processes
{
    public interface IProcessesRepository
    {
        Task<IEnumerable<Process>> GetAllAsync(CancellationToken cancellationToken);
    }
}