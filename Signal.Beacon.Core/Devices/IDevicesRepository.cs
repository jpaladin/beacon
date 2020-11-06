using System.Threading.Tasks;

namespace Signal.Beacon.Core.Devices
{
    public interface IDevicesRepository
    {
        Task<DeviceConfiguration?> GetAsync(string identifier);
    }
}