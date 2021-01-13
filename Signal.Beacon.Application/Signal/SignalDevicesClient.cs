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
        private const string SignalApiDevicesEdnpointsUpdateUrl = "/devices/endpoints/update";
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

            return response.Select(d => new DeviceConfiguration(
                d.Id,
                d.Alias,
                d.DeviceIdentifier,
                MapEndpointsFromDto(d.Endpoints ?? Enumerable.Empty<SignalDeviceEndpointDto>()))
            {
                Manufacturer = d.Manufacturer,
                Model = d.Model
            });
        }

        public async Task UpdateDeviceAsync(string deviceId, DeviceDiscoveredCommand command, CancellationToken cancellationToken)
        {
            await this.client.PostAsJsonAsync(
                SignalApiDevicesEdnpointsUpdateUrl,
                new SignalDeviceEndpointsUpdateDto(
                    deviceId,
                    MapEndpointsToDto(command.Endpoints)),
                cancellationToken);
        }

        public async Task<string> RegisterDeviceAsync(DeviceDiscoveredCommand discoveredDevice, CancellationToken cancellationToken)
        {
            var response = await this.client.PostAsJsonAsync<SignalDeviceRegisterDto, SignalDeviceRegisterResponseDto>(
                SignalApiDevicesRegisterUrl,
                new SignalDeviceRegisterDto(
                    discoveredDevice.Identifier,
                    discoveredDevice.Alias,
                    MapEndpointsToDto(discoveredDevice.Endpoints),
                    discoveredDevice.Manufacturer,
                    discoveredDevice.Model),
                cancellationToken);

            if (response == null)
                throw new Exception("Didn't get valid response for device registration.");

            return response.DeviceId;
        }

        private static IEnumerable<DeviceEndpoint> MapEndpointsFromDto(
            IEnumerable<SignalDeviceEndpointDto> endpoints) =>
            endpoints.Select(e => new DeviceEndpoint(e.Channel, e.Contacts.Select(c =>
                new DeviceContact(c.Name, c.DataType, (DeviceContactAccess) c.Access)
                {
                    NoiseReductionDelta = c.NoiseReductionDelta
                })));

        private static IEnumerable<SignalDeviceEndpointDto> MapEndpointsToDto(IEnumerable<DeviceEndpoint> endpoints) =>
            endpoints.Select(e =>
                new SignalDeviceEndpointDto(
                    e.Channel,
                    e.Contacts.Select(c => new SignalDeviceEndpointContactDto(
                        c.Name,
                        c.DataType,
                        (SignalDeviceEndpointContactAccessDto) c.Access,
                        c.NoiseReductionDelta))));

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
}