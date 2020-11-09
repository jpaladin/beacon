using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signal.Beacon.Core.Devices
{
    public interface IDevicesRepository
    {
        Task<DeviceConfiguration?> GetAsync(string identifier);
        
        Task<IEnumerable<DeviceConfiguration>> GetAllAsync();
    }
}