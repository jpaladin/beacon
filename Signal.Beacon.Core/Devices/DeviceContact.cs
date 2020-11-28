namespace Signal.Beacon.Core.Devices
{
    public record DeviceContact(string Name, string DataType)
    {
        public bool IsReadonly { get; init; }
    }
}