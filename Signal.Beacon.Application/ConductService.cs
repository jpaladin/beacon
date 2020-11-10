using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.MessageQueue;

namespace Signal.Beacon.Application
{
    public class ConductService : IConductService
    {
        private readonly IMqttClient client;
        private readonly ILogger<ConductService> logger;

        public ConductService(IMqttClient client, ILogger<ConductService> logger)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishConductsAsync(string wireIdentifier, IEnumerable<Conduct> conducts)
        {
            foreach (var conduct in conducts) 
                await this.PublishConduct(wireIdentifier, conduct);
        }

        private async Task PublishConduct(string wireIdentifier, Conduct conduct)
        {
            await this.client.PublishAsync($"signal/conducts/{wireIdentifier}", conduct);
            this.logger.LogDebug("Conduct published: {Wire} {@Conduct}", wireIdentifier, conduct);
        }
    }
}