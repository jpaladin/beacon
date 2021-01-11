using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Signal;

namespace Signal.Beacon.Application.Signal
{
    internal class SignalDevicesClient : ISignalDevicesClient
    {
        private const string SignalApiDevicesGetUrl = "/devices";
        private const string SignalApiDevicesRegisterUrl = "/devices/register";
        private const string SignalApiDevicesStatePublishUrl = "/devices/state";

        private readonly ISignalClient client;

        public SignalDevicesClient(
            ISignalClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<IEnumerable<DeviceConfiguration>> GetDevicesAsync(CancellationToken cancellationToken)
        {
            var response = await this.client.GetAsync<IEnumerable<SignalDeviceDto>>(
                SignalApiDevicesGetUrl,
                cancellationToken);
            if (response == null)
                throw new Exception("Failed to retrieve devices from API.");

            return response.Select(d => new DeviceConfiguration(d.Id, d.Alias, d.DeviceIdentifier));
        }

        public async Task<string> RegisterDeviceAsync(DeviceDiscoveredCommand discoveredDevice, CancellationToken cancellationToken)
        {
            var response = await this.client.PostAsJsonAsync<SignalDeviceRegisterDto, SignalDeviceRegisterResponseDto>(
                SignalApiDevicesRegisterUrl,
                new SignalDeviceRegisterDto(discoveredDevice.Identifier, discoveredDevice.Alias),
                cancellationToken);
            if (response == null)
                throw new Exception("Didn't get valid response for device registration.");

            return response.DeviceId;
        }

        public async Task DevicesPublishStateAsync(string deviceId, DeviceTarget target, object? value, DateTime timeStamp, CancellationToken cancellationToken)
        {
            var (channel, _, contact) = target;
            var data = new SignalDeviceStatePublishDto
            (
                deviceId,
                channel,
                contact,
                this.SerializeValue(value),
                timeStamp
            );

            await this.client.PostAsJsonAsync(SignalApiDevicesStatePublishUrl, data, cancellationToken);
        }
    }

    public class SignalDeviceDto
    {
        public string? Id { get; set; }

        public string? DeviceIdentifier { get; set; }

        public string? Alias { get; set; }
    }
}