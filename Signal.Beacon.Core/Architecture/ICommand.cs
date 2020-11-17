using System;

namespace Signal.Beacon.Core.Devices
{
    public interface ICommand
    {
        Guid Id { get; }
    }
}