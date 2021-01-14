using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Application.Conducts;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application.Signal.SignalR
{
    public interface ISignalSignalRClient
    {
        Task StartAsync(CancellationToken cancellationToken);
    }

    internal class SignalSignalRClient : ISignalSignalRClient
    {
        private readonly ISignalClientAuthFlow signalClientAuthFlow;
        private readonly IDevicesDao devicesDao;
        private readonly IConductManager conductManager;
        private readonly ILogger<SignalSignalRClient> logger;
        private HubConnection? conductsConnection;
        private CancellationToken? startCancellationToken;

        public SignalSignalRClient(
            ISignalClientAuthFlow signalClientAuthFlow,
            IDevicesDao devicesDao,
            IConductManager conductManager,
            ILogger<SignalSignalRClient> logger)
        {
            this.signalClientAuthFlow = signalClientAuthFlow ?? throw new ArgumentNullException(nameof(signalClientAuthFlow));
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
            this.conductManager = conductManager ?? throw new ArgumentNullException(nameof(conductManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.startCancellationToken = cancellationToken;
            await this.ConnectAsync(cancellationToken);
        }

        // TODO: Move
        private record ConductRequestDto(
            string DeviceId, 
            string ChannelName, 
            string ContactName,
            string ValueSerialized);

        private async Task ConductRequestedHandlerAsync(string payload)
        {
            var request = JsonSerializer.Deserialize<ConductRequestDto>(payload);

            this.logger.LogInformation("Conduct requested: {DeviceId} {ChannelName} {ContactName} {ValueSerialized}",
                request.DeviceId,
                request.ChannelName,
                request.ContactName,
                request.ValueSerialized);

            if (this.startCancellationToken == null ||
                this.startCancellationToken.Value.IsCancellationRequested)
                return;

            var device = await this.devicesDao.GetByIdAsync(request.DeviceId, this.startCancellationToken.Value);
            if (device != null)
                await this.conductManager.PublishAsync(new[]
                {
                    new Conduct(new DeviceTarget(request.ChannelName, device.Identifier, request.ContactName),
                        request.ValueSerialized)
                }, this.startCancellationToken.Value);
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Wait for SignalRClient to assign token
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (await this.signalClientAuthFlow.GetTokenAsync(cancellationToken) != null)
                        break;
                    await Task.Delay(1000, cancellationToken);
                }

                var accessToken = (await this.signalClientAuthFlow.GetTokenAsync(cancellationToken)).AccessToken;
                this.conductsConnection = new HubConnectionBuilder()
                    .WithUrl("https://signal-api.azurewebsites.net/api/signalr/conducts", options =>
                    {
                        options.Headers["Authorization"] =
                            $"Bearer {accessToken}";
                    })
                    .WithAutomaticReconnect()
                    .Build();

                this.conductsConnection.Closed += async error =>
                {
                    this.logger.LogInformation(error, "Conducts hub connection closed. Reconnecting...");
                };

                this.conductsConnection.On<string>("requested", this.ConductRequestedHandlerAsync);

                // Start the connection
                await this.conductsConnection.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to connect to Signal SignalR Conducts hub.");
                await Task.Delay(1000, cancellationToken);
                _ = this.ConnectAsync(cancellationToken);
            }
        }
    }
}
