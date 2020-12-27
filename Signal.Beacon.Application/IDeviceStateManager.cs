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

        Task<object?> GetStateAsync(DeviceTarget target);

        Task<IEnumerable<IHistoricalValue>?> GetStateHistoryAsync(
            DeviceTarget target,
            DateTime startTimeStamp, 
            DateTime endTimeStamp);
    }
}