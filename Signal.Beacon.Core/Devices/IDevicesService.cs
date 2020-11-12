using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Core.Devices
{
    public interface IDevicesService
    {
        Task<DeviceConfiguration?> GetAsync(string identifier);

        void SetState(DeviceTarget target, object? value);

        Task<object?> GetStateAsync(DeviceTarget target);
        
        Task<IEnumerable<DeviceConfiguration>> GetAllAsync();

        Task<IEnumerable<IHistoricalValue>?> GetStateHistoryAsync(
            DeviceTarget target, 
            DateTime startTimeStamp,
            DateTime endTimeStamp);
    }
}