using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Application
{
    public interface IDeviceStateManager
    {
        void SetState(DeviceTarget target, object? value);

        Task<object?> GetStateAsync(DeviceTarget target);

        Task<IEnumerable<IHistoricalValue>?> GetStateHistoryAsync(
            DeviceTarget target,
            DateTime startTimeStamp, 
            DateTime endTimeStamp);
    }
}