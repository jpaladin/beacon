using System;
using System.Collections.Generic;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application.Signal
{
    [Flags]
    public enum SignalDeviceEndpointContactAccessDto
    {
        None = 0x0,
        Read = 0x1,
        Write = 0x2,
        Get = 0x4
    }

    public record SignalDeviceEndpointContactDto(
        string Name,
        string DataType,
        SignalDeviceEndpointContactAccessDto Access,
        double? NoiseReductionDelta);

    public record SignalDeviceEndpointDto(
        string Channel,
        IEnumerable<SignalDeviceEndpointContactDto> Contacts);

    public record SignalDeviceEndpointsUpdateDto(
        string DeviceId,
        IEnumerable<SignalDeviceEndpointDto> Endpoints);

    public record SignalDeviceRegisterDto(
        string DeviceIdentifier, 
        string Alias,
        IEnumerable<SignalDeviceEndpointDto> Endpoints, 
        string? Manufacturer, 
        string? Model);

    public record SignalDeviceRegisterResponseDto(string DeviceId);

    public record SignalDeviceStatePublishDto(
        string DeviceId, string ChannelName, string ContactName,
        string? ValueSerialized, DateTime TimeStamp);
}