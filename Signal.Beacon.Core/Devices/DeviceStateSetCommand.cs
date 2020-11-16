using System;

namespace Signal.Beacon.Core.Devices
{
    public class DeviceStateSetCommand : IDeviceStateCommand
    {
        public DeviceStateSetCommand(DeviceTarget target, object? value)
        {
            this.Id = Guid.NewGuid();
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.Value = value;
        }

        public Guid Id { get; }

        public DeviceTarget Target { get; }

        public object? Value { get; }
    }
}