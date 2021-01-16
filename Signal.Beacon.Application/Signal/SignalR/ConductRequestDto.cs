namespace Signal.Beacon.Application.Signal.SignalR
{
    public record ConductRequestDto(
        string DeviceId,
        string ChannelName,
        string ContactName,
        string ValueSerialized);
}