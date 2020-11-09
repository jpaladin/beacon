using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application
{
    public class DevicesRepository : IDevicesRepository
    {
        private readonly IConfigurationService configurationService;
        private Dictionary<string, DeviceConfiguration>? devices;

        public DevicesRepository(IConfigurationService configurationService)
        {
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task<DeviceConfiguration?> GetAsync(string identifier)
        {
            await this.CacheDevicesAsync();

            if (this.devices != null && 
                this.devices.TryGetValue(identifier, out var device))
                return device;
            return null;
        }

        public Task<IEnumerable<DeviceConfiguration>> GetAllAsync() => 
            Task.FromResult(this.devices?.Values.AsEnumerable() ?? Enumerable.Empty<DeviceConfiguration>());

        private async Task CacheDevicesAsync()
        {
            if (this.devices != null)
                return;

            this.devices = (await this.configurationService.LoadDevicesAsync()).ToDictionary(d => d.Identifier);
        }
    }
}