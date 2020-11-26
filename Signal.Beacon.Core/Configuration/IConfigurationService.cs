using System.Collections.Generic;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Core.Configuration
{
    public interface IConfigurationService
    {
        Task<IEnumerable<DeviceConfiguration>> LoadDevicesAsync();

        Task<IEnumerable<Process>> LoadProcessesAsync();
        Task<T> LoadAsync<T>(string name) where T : new();
        Task SaveAsync<T>(string name, T config);
    }
}