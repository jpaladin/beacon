using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Application.PubSub;
using Signal.Beacon.Core.Conducts;

namespace Signal.Beacon.Application.Conducts
{
    internal class ConductManager : IConductManager
    {
        private readonly IPubSubTopicHub<Conduct> conductHub;
        private readonly ILogger<ConductManager> logger;


        public ConductManager(
            IPubSubTopicHub<Conduct> conductHub,
            ILogger<ConductManager> logger)
        {
            this.conductHub = conductHub ?? throw new ArgumentNullException(nameof(conductHub));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public IDisposable Subscribe(string channel, Func<Conduct, CancellationToken, Task> handler) =>
            this.conductHub.Subscribe(new[] {channel}, handler);

        public async Task PublishAsync(IEnumerable<Conduct> conducts, CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                conducts
                    .GroupBy(c => c.Target.Channel)
                    .Select(cGroup => this.conductHub.PublishAsync(cGroup.Key, cGroup, cancellationToken)));
        }
    }
}
