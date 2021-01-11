using Signal.Beacon.Core.Architecture;

namespace Signal.Beacon.Core.Devices
{
    public interface IDeviceStateCommand : ICommand
    {
        DeviceTarget Target { get; }
    }
}