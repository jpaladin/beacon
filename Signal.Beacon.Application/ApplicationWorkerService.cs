using System;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Application.Conducts;
using Signal.Beacon.Application.Processing;
using Signal.Beacon.Application.Signal.SignalR;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.Application
{
    internal class ApplicationWorkerService : IWorkerService
    {
        private readonly IProcessor processor;
        private readonly ISignalSignalRDevicesHubClient devicesHubClient;
        private readonly ISignalSignalRConductsHubClient conductsHubClient;
        private readonly IConductManager conductManager;

        public ApplicationWorkerService(
            IProcessor processor,
            ISignalSignalRDevicesHubClient devicesHubClient,
            ISignalSignalRConductsHubClient conductsHubClient,
            IConductManager conductManager)
        {
            this.processor = processor ?? throw new ArgumentNullException(nameof(processor));
            this.devicesHubClient = devicesHubClient ?? throw new ArgumentNullException(nameof(devicesHubClient));
            this.conductsHubClient = conductsHubClient ?? throw new ArgumentNullException(nameof(conductsHubClient));
            this.conductManager = conductManager ?? throw new ArgumentNullException(nameof(conductManager));
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.devicesHubClient.StartAsync(cancellationToken);
            await this.conductsHubClient.StartAsync(cancellationToken);
            await this.processor.StartAsync(cancellationToken);
            await this.conductManager.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}