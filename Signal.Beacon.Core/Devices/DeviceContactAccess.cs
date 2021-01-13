using System;

namespace Signal.Beacon.Core.Devices
{
    [Flags]
    public enum DeviceContactAccess
    {
        None = 0x0,
        Read = 0x1,
        Write = 0x2,
        Get = 0x4
    }
}