using System;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Application.Processing;
using Signal.Beacon.Application.Signal.SignalR;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.Application
{
    public class ApplicationWorkerService : IWorkerService
    {
        private readonly IProcessor processor;
        private readonly ISignalSignalRClient signalRClient;

        public ApplicationWorkerService(
            IProcessor processor,
            ISignalSignalRClient signalRClient)
        {
            this.processor = processor ?? throw new ArgumentNullException(nameof(processor));
            this.signalRClient = signalRClient ?? throw new ArgumentNullException(nameof(signalRClient));
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.processor.StartAsync(cancellationToken);
            await this.signalRClient.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}