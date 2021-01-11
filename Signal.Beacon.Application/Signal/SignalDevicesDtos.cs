using System;

namespace Signal.Beacon.Application.Signal
{
    public record SignalDeviceRegisterDto(string DeviceIdentifier, string Alias);

    public record SignalDeviceRegisterResponseDto(string DeviceId);

    public record SignalDeviceStatePublishDto(
        string DeviceId, string ChannelName, string ContactName,
        string? ValueSerialized, DateTime TimeStamp);
}