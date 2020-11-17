using Signal.Beacon.Application;

namespace Signal.Beacon.Core.Devices
{
    internal interface IDevicesCommandHandler :
        ICommandHandler<DeviceStateSetCommand>,
        ICommandHandler<DeviceDiscoveredCommand>
    {
    }
}