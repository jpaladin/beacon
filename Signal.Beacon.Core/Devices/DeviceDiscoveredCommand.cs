using System;

namespace Signal.Beacon.Core.Devices
{
    public class DeviceDiscoveredCommand : ICommand
    {
        public DeviceDiscoveredCommand(DeviceConfiguration device)
        {
            this.Id = Guid.NewGuid();
            this.Device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public DeviceConfiguration Device { get; set; }
        public Guid Id { get; }
    }
}