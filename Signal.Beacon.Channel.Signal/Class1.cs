using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Signal;

namespace Signal.Beacon.Channel.Signal
{

    public static class SignalExtensions
    {
        public static IServiceCollection AddSignalApi(this IServiceCollection services)
        {
            return services.AddSingleton<ISignalClient, SignalClient>();
        }
    }
    
    public class SignalBeaconRegisterDto
    {
        public string BeaconId { get; set; }
    }
    
    public class SignalDeviceStatePublishDto
    {
        public string DeviceIdentifier { get; set; }
        
        public string ChannelName { get; set; }
        
        public string ContactName { get; set; }
        
        public string? ValueSerialized { get; set; }
        
        public DateTime TimeStamp { get; set; }
    }
    
    public class SignalClient : ISignalClient
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
            var data = new SignalBeaconRegisterDto
            {
                BeaconId = beaconId
            };
            await this.PostAsJsonAsync(SignalApiBeaconRegisterUrl, data, cancellationToken);
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
