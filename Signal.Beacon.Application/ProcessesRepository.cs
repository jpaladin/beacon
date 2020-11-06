using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Application
{
    public class ProcessesRepository : IProcessesRepository
    {
        private readonly IConfigurationService configurationService;
        private IEnumerable<Process>? processes;

        public ProcessesRepository(
            IConfigurationService configurationService)
        {
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task<IEnumerable<Process>> GetAllAsync()
        {
            await this.CacheProcessesAsync();
            return this.processes ?? throw new Exception("Couldn't retrieve processes because cache is empty.");
        }

        private async Task CacheProcessesAsync() => 
            this.processes ??= await this.configurationService.LoadProcessesAsync();
    }
}