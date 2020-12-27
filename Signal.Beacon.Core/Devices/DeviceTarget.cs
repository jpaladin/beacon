namespace Signal.Beacon.Core.Devices
{
    public record DeviceTarget(string Identifier, string Contact, string Channel = "main");
}