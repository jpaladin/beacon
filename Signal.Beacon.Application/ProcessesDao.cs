using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Application
{
    public class ProcessesDao : IProcessesDao
    {
        private readonly IConfigurationService configurationService;
        private IEnumerable<Process>? processes;

        public ProcessesDao(
            IConfigurationService configurationService)
        {
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task<IEnumerable<Process>> GetAllAsync(CancellationToken cancellationToken)
        {
            await this.CacheProcessesAsync(cancellationToken);

            return this.processes ?? Enumerable.Empty<Process>();
        }

        private async Task CacheProcessesAsync(CancellationToken cancellationToken)
        {
            if (this.processes != null)
                return;

            this.processes = await this.configurationService.LoadAsync<List<Process>>("Processes.json", cancellationToken);
        }
    }
}