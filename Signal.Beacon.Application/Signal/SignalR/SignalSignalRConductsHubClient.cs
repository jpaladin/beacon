using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Signal.Beacon.Application.Signal.SignalR
{
    internal class SignalSignalRConductsHubClient : SignalSignalRHubHubClient, ISignalSignalRConductsHubClient
    {
        private readonly ILogger<SignalSignalRConductsHubClient> logger;

        public SignalSignalRConductsHubClient(
            ISignalClientAuthFlow signalClientAuthFlow, 
            ILogger<SignalSignalRHubHubClient> logger,
            ILogger<SignalSignalRConductsHubClient> conductsLogger) : 
            base(signalClientAuthFlow, logger)
        {
            this.logger = conductsLogger ?? throw new ArgumentNullException(nameof(conductsLogger));
        }

        public override Task StartAsync(CancellationToken cancellationToken) => 
            this.StartAsync("conducts", cancellationToken);

        public async Task OnConductRequestAsync(Func<ConductRequestDto, CancellationToken, Task> handler, CancellationToken cancellationToken)
        {
            await this.OnAsync<string>("requested", async payload =>
            {
                var request = JsonSerializer.Deserialize<ConductRequestDto>(payload);
                if (request == null)
                {
                    this.logger.LogDebug("Got empty conduct request from SignalR. Payload: {Payload}", payload);
                    return;
                }

                this.logger.LogInformation("Conduct requested: {DeviceId} {ChannelName} {ContactName} {ValueSerialized}",
                    request.DeviceId,
                    request.ChannelName,
                    request.ContactName,
                    request.ValueSerialized);

                if (this.StartCancellationToken == null ||
                    this.StartCancellationToken.Value.IsCancellationRequested)
                    return;

                await handler(request, this.StartCancellationToken.Value);
            }, cancellationToken);
        }
    }
}