using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application
{
    public interface IDeviceStateManager
    {
        void SetState(DeviceTarget target, object? value);

        Task<object?> GetStateAsync(DeviceTarget target);
    }
}