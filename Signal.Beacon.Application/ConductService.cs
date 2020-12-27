using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Core.Conducts;

namespace Signal.Beacon.Application
{
    public class ConductService : IConductService
    {
        private readonly ILogger<ConductService> logger;

        public ConductService(ILogger<ConductService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishConductsAsync(IEnumerable<Conduct> conducts, CancellationToken cancellationToken)
        {
            foreach (var conduct in conducts) 
                await this.PublishConduct(conduct, cancellationToken);
        }

        private async Task PublishConduct(Conduct conduct, CancellationToken cancellationToken)
        {
            // TODO: Publish conduct locally
            // TODO: Publish to cloud if not resolved locally
            // TODO: Notify cloud conduct executed
            this.logger.LogDebug("Conduct published: {@Conduct}", conduct);
        }
    }
}