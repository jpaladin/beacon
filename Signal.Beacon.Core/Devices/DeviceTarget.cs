namespace Signal.Beacon.Core.Devices
{
    public record DeviceTarget(string Identifier, string? Contact = null, string Channel = "main");
}