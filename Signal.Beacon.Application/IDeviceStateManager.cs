using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Application
{
    public interface IDeviceStateManager
    {
        Task SetStateAsync(DeviceTarget target, object? value, CancellationToken cancellationToken);

        Task<object?> GetStateAsync(DeviceContactTarget target);

        Task<IEnumerable<IHistoricalValue>?> GetStateHistoryAsync(DeviceContactTarget target,
            DateTime startTimeStamp,
            DateTime endTimeStamp);
    }
}