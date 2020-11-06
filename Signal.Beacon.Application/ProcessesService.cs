using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Application
{
    public class ProcessesService : IProcessesService
    {
        private readonly IProcessesRepository processesRepository;

        public ProcessesService(IProcessesRepository processesRepository)
        {
            this.processesRepository = processesRepository ?? throw new ArgumentNullException(nameof(processesRepository));
        }

        public Task<IEnumerable<Process>> GetAllAsync() => this.processesRepository.GetAllAsync();
    }
}