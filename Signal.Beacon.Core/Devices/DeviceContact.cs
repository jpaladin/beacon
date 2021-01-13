namespace Signal.Beacon.Core.Devices
{
    public record DeviceContact(string Name, string DataType, DeviceContactAccess Access)
    {
        public double? NoiseReductionDelta { get; init; }
    }
}