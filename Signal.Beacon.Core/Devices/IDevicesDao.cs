using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Core.Devices
{
    public interface IDevicesDao
    {
        Task<DeviceConfiguration?> GetAsync(string identifier);
        
        Task<IEnumerable<DeviceConfiguration>> GetAllAsync();
        
        Task<IEnumerable<IHistoricalValue>?> GetStateHistoryAsync(DeviceTarget deviceTarget, DateTime startTimeStamp, DateTime endTimeStamp);

        Task<object?> GetStateAsync(DeviceTarget deviceTarget);
        Task UpdateDeviceAsync(string deviceIdentifier, DeviceConfiguration deviceConfiguration);
        Task<DeviceConfiguration?> GetByAliasAsync(string alias);
    }
}