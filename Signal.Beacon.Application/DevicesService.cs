using System;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application
{
    public class DevicesService : IDevicesService
    {
        private readonly IDeviceStateManager deviceStateManager;
        private readonly IDevicesRepository devicesRepository;

        public DevicesService(
            IDeviceStateManager deviceStateManager,
            IDevicesRepository devicesRepository)
        {
            this.deviceStateManager = deviceStateManager ?? throw new ArgumentNullException(nameof(deviceStateManager));
            this.devicesRepository = devicesRepository ?? throw new ArgumentNullException(nameof(devicesRepository));
        }

        public Task<DeviceConfiguration?> GetAsync(string identifier) => 
            this.devicesRepository.GetAsync(identifier);

        public void SetState(DeviceTarget target, object? value) => 
            this.deviceStateManager.SetState(target, value);

        public Task<object?> GetStateAsync(DeviceTarget target) => 
            this.deviceStateManager.GetStateAsync(target);
    }
}