using System;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Signal;

namespace Signal.Beacon.Application.Signal
{
    internal class SignalBeaconClient : ISignalBeaconClient
    {
        private const string SignalApiBeaconRegisterUrl = "/beacons/register";

        private readonly ISignalClient client;

        public SignalBeaconClient(
            ISignalClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task RegisterBeaconAsync(string beaconId, CancellationToken cancellationToken)
        {
            await this.client.PostAsJsonAsync(
                SignalApiBeaconRegisterUrl,
                new SignalBeaconRegisterRequestDto(beaconId),
                cancellationToken);
        }
    }
}