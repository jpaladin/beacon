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
    }
}