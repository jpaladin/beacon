using Signal.Beacon.Core.Architecture;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application
{
    internal interface IDevicesCommandHandler :
        ICommandHandler<DeviceStateSetCommand>,
        ICommandHandler<DeviceDiscoveredCommand>
    {
    }
}