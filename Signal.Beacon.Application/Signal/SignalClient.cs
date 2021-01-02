using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Application.Auth;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Signal;

namespace Signal.Beacon.Application.Signal
{
    public class SignalClient : ISignalClient, ISignalClientAuthFlow
    {
        private const string SignalApiUrl = "https://signal-api.azurewebsites.net";
        //private const string SignalApiUrl = "http://localhost:7071";

        private static readonly string SignalApiBeaconRegisterUrl = $"{SignalApiUrl}/api/beacons/register";
        private static readonly string SignalApiDevicesStatePublishUrl = $"{SignalApiUrl}/api/devices/state";
        
        private readonly HttpClient client = new();
        private AuthToken? token;
                
        public void AssignToken(AuthToken newToken)
        {
            this.token = newToken;
            this.client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", this.token.AccessToken);
        }

        public Task RenewTokenAsync()
        {
            throw new NotImplementedException();
        }

        public async Task DevicesPublishStateAsync(DeviceTarget target, object? value, DateTime timeStamp, CancellationToken cancellationToken)
        {
            var (channel, identifier, contact) = target;
            var data = new SignalDeviceStatePublishDto
            {
                DeviceIdentifier = identifier,
                ChannelName = channel,
                ContactName = contact,
                TimeStamp = timeStamp,
                ValueSerialized = SerializeValue(value)
            };
            
            await this.PostAsJsonAsync(SignalApiDevicesStatePublishUrl, data, cancellationToken);
        }

        private static string? SerializeValue(object? value) =>
            value switch
            {
                null => null,
                string stringValue => stringValue,
                _ => JsonSerializer.Serialize(value)
            };

        public async Task RegisterBeaconAsync(string beaconId, CancellationToken cancellationToken)
        {
            await this.PostAsJsonAsync(
                SignalApiBeaconRegisterUrl, 
                new SignalBeaconRegisterDto(beaconId),
                cancellationToken);
        }

        private async Task PostAsJsonAsync<T>(string url, T data, CancellationToken cancellationToken)
        {
            using var response = await this.client.PostAsJsonAsync(url, data, cancellationToken);
            // TODO: Handle token refresh
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Signal API POST {url} failed. Reason: {await response.Content.ReadAsStringAsync(cancellationToken)} ({response.StatusCode})");
        }
    }
}