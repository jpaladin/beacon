using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Application.Auth;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Signal;

namespace Signal.Beacon.Application.Signal
{
    public class SignalClient : ISignalClient, ISignalClientAuthFlow
    {
        private readonly ILogger<SignalClient> logger;

        private const string SignalApiUrl = "https://signal-api.azurewebsites.net";
        //private const string SignalApiUrl = "http://localhost:7071";

        private static readonly string SignalApiBeaconRegisterUrl = $"{SignalApiUrl}/api/beacons/register";
        private static readonly string SignalApiBeaconRefreshTokenUrl = $"{SignalApiUrl}/api/beacons/refresh-token";
        private static readonly string SignalApiDevicesStatePublishUrl = $"{SignalApiUrl}/api/devices/state";
        
        private readonly HttpClient client = new();
        private AuthToken? token;


        public SignalClient(ILogger<SignalClient> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public void AssignToken(AuthToken newToken)
        {
            this.token = newToken;
            this.client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", this.token.AccessToken);

            this.logger.LogDebug("Token successfully assigned. Expires on: {TokenExpire}", this.token.Expire);
        }

        private async Task RenewTokenIfExpiredAsync(CancellationToken cancellationToken)
        {
            // Can't renew unassigned token (used for unauthenticated requests)
            if (this.token == null)
                return;

            // Not expired
            if (DateTime.UtcNow < this.token.Expire)
                return;

            // Request new token from Signal API
            var response = await this.PostAsJsonAsync<SignalBeaconRefreshTokenRequestDto, SignalBeaconRefreshTokenResponseDto>(
                SignalApiBeaconRefreshTokenUrl,
                new SignalBeaconRefreshTokenRequestDto(this.token.RefreshToken), 
                cancellationToken,
                false);
            if (response == null)
                throw new Exception("Failed to retrieve refreshed token.");

            // Assign new token
            this.AssignToken(new AuthToken(response.AccessToken, this.token.RefreshToken, response.Expire));
            this.logger.LogDebug("Token successfully refreshed. Expires on: {TokenExpire}", this.token.Expire);
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
                new SignalBeaconRegisterRequestDto(beaconId),
                cancellationToken);
        }

        private async Task PostAsJsonAsync<T>(string url, T data, CancellationToken cancellationToken)
        {
            await this.RenewTokenIfExpiredAsync(cancellationToken);

            using var response = await this.client.PostAsJsonAsync(url, data, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Signal API POST {url} failed. Reason: {await response.Content.ReadAsStringAsync(cancellationToken)} ({response.StatusCode})");
        }

        private async Task<TResponse?> PostAsJsonAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken cancellationToken, bool renewTokenIfExpired = true)
        {
            if (renewTokenIfExpired)
                await this.RenewTokenIfExpiredAsync(cancellationToken);

            using var response = await this.client.PostAsJsonAsync(url, data, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Signal API POST {url} failed. Reason: {await response.Content.ReadAsStringAsync(cancellationToken)} ({response.StatusCode})");
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true}, 
                cancellationToken);
        }
    }
}