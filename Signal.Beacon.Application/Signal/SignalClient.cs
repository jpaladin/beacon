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

namespace Signal.Beacon.Application.Signal
{
    internal class SignalClient : ISignalClient, ISignalClientAuthFlow
    {
        private const string SignalApiUrl = "https://signal-api.azurewebsites.net/api";
        //private const string SignalApiUrl = "http://localhost:7071";

        private static readonly string SignalApiBeaconRefreshTokenUrl = "/beacons/refresh-token";

        private readonly ILogger<SignalClient> logger;
        private readonly HttpClient client = new();
        private AuthToken? token;
        private Task<SignalBeaconRefreshTokenResponseDto?>? renewTokenTask;


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

        public async Task<AuthToken?> GetTokenAsync(CancellationToken cancellationToken)
        {
            await this.RenewTokenIfExpiredAsync(cancellationToken);
            return this.token;
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
            this.renewTokenTask ??= this.PostAsJsonAsync<SignalBeaconRefreshTokenRequestDto, SignalBeaconRefreshTokenResponseDto>(
                SignalApiBeaconRefreshTokenUrl,
                new SignalBeaconRefreshTokenRequestDto(this.token.RefreshToken),
                cancellationToken,
                false);

            // Wait for response
            var response = await this.renewTokenTask;
            if (response == null)
                throw new Exception("Failed to retrieve refreshed token.");

            // Check if someone else assigned new token already
            if (DateTime.UtcNow < this.token.Expire)
                return;

            try
            {
                // Assign new token
                this.AssignToken(new AuthToken(response.AccessToken, this.token.RefreshToken, response.Expire));
                this.logger.LogDebug("Token successfully refreshed. Expires on: {TokenExpire}", this.token.Expire);
            }
            finally
            {
                this.renewTokenTask = null;
            }
        }

        public async Task PostAsJsonAsync<T>(string url, T data, CancellationToken cancellationToken)
        {
            await this.RenewTokenIfExpiredAsync(cancellationToken);

            using var response = await this.client.PostAsJsonAsync($"{SignalApiUrl}{url}", data, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Signal API POST {SignalApiUrl}{url} failed. Reason: {await response.Content.ReadAsStringAsync(cancellationToken)} ({response.StatusCode})");
        }

        public async Task<TResponse?> PostAsJsonAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken cancellationToken, bool renewTokenIfExpired = true)
        {
            if (renewTokenIfExpired)
                await this.RenewTokenIfExpiredAsync(cancellationToken);

            using var response = await this.client.PostAsJsonAsync($"{SignalApiUrl}{url}", data, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    throw new Exception($"API returned NOCONTENT but we expected response of type {typeof(TResponse).FullName}");

                try
                {
                    var responseData = await response.Content.ReadFromJsonAsync<TResponse>(
                        new JsonSerializerOptions {PropertyNameCaseInsensitive = true},
                        cancellationToken);
                    return responseData;
                }
                catch (JsonException ex)
                {
                    var responseDataString = await response.Content.ReadAsStringAsync(cancellationToken);
                    this.logger.LogTrace(ex, "Failed to read response JSON.");
                    this.logger.LogDebug("Reading response JSON failed. Raw: {DataString}", responseDataString);
                    throw;
                }
            }

            var responseContent = await this.GetResponseContentStringAsync(response, cancellationToken);
            throw new Exception($"Signal API POST {SignalApiUrl}{url} failed. Reason: {responseContent} ({response.StatusCode})");
        }

        public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
        {
            await this.RenewTokenIfExpiredAsync(cancellationToken);

            return await this.client.GetFromJsonAsync<T>(
                $"{SignalApiUrl}{url}", 
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true},
                cancellationToken);
        }

        private async Task<string> GetResponseContentStringAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            try
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                return string.IsNullOrWhiteSpace(responseString) ? "No content." : responseString;
            }
            catch (Exception ex)
            {
                this.logger.LogDebug(ex, "Failed to read API response content.");
                return "Failed to read API response content.";
            }
        }
    }
}