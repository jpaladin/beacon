using System.Threading.Tasks;

namespace Signal.Beacon.Core.Devices
{
    public interface IDevicesService
    {
        Task<DeviceConfiguration?> GetAsync(string identifier);

        void SetState(DeviceTarget target, object? value);

        Task<object?> GetStateAsync(DeviceTarget target);
    }
}