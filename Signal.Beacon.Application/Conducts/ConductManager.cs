using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Application.PubSub;
using Signal.Beacon.Application.Signal.SignalR;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application.Conducts
{
    internal class ConductManager : IConductManager
    {
        private readonly IPubSubTopicHub<Conduct> conductHub;
        private readonly ISignalSignalRConductsHubClient signalRConductsHubClient;
        private readonly IDevicesDao devicesDao;
        private readonly ILogger<ConductManager> logger;


        public ConductManager(
            IPubSubTopicHub<Conduct> conductHub,
            ISignalSignalRConductsHubClient signalRConductsHubClient,
            IDevicesDao devicesDao,
            ILogger<ConductManager> logger)
        {
            this.conductHub = conductHub ?? throw new ArgumentNullException(nameof(conductHub));
            this.signalRConductsHubClient = signalRConductsHubClient ?? throw new ArgumentNullException(nameof(signalRConductsHubClient));
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task ConductRequestedHandlerAsync(ConductRequestDto request, CancellationToken cancellationToken)
        {
            var device = await this.devicesDao.GetByIdAsync(request.DeviceId, cancellationToken);
            if (device != null)
                await this.PublishAsync(new[]
                {
                    new Conduct(
                        new DeviceTarget(request.ChannelName, device.Identifier, request.ContactName),
                        request.ValueSerialized)
                }, cancellationToken);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.signalRConductsHubClient.OnConductRequestAsync(this.ConductRequestedHandlerAsync, cancellationToken);
        }

        public IDisposable Subscribe(string channel, Func<Conduct, CancellationToken, Task> handler) =>
            this.conductHub.Subscribe(new[] {channel}, handler);

        public async Task PublishAsync(IEnumerable<Conduct> conducts, CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                conducts
                    .GroupBy(c => c.Target.Channel)
                    .Select(cGroup => this.conductHub.PublishAsync(cGroup.Key, cGroup, cancellationToken)));

            // TODO: Publish to SignalR if no local handler successfully handled the conduct
        }
    }
}
