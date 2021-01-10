using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Extensions;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Application
{
    public class DevicesDao : IDevicesDao
    {
        private readonly Lazy<IDeviceStateManager> deviceStateManager;
        private Dictionary<string, DeviceConfiguration>? devices;

        public DevicesDao(
            Lazy<IDeviceStateManager> deviceStateManager)
        {
            this.deviceStateManager = deviceStateManager ?? throw new ArgumentNullException(nameof(deviceStateManager));
        }

        public async Task<DeviceConfiguration?> GetByAliasAsync(string alias, CancellationToken cancellationToken)
        {
            await this.CacheDevicesAsync(cancellationToken);

            return this.devices?.Values.FirstOrDefault(d => d.Alias == alias);
        }

        public async Task<DeviceContact?> GetInputContactAsync(DeviceTarget target, CancellationToken cancellationToken)
        {
            await this.CacheDevicesAsync(cancellationToken);

            var device = await this.GetAsync(target.Identifier, cancellationToken);
            return device?.Endpoints
                .Where(d => d.Channel == target.Channel)
                .SelectMany(d => d.Inputs)
                .FirstOrDefault(c => c.Name == target.Contact);
        }

        public async Task<DeviceConfiguration?> GetAsync(string identifier, CancellationToken cancellationToken)
        {
            await this.CacheDevicesAsync(cancellationToken);

            if (this.devices != null && 
                this.devices.TryGetValue(identifier, out var device))
                return device;
            return null;
        }

        public async Task<IEnumerable<DeviceConfiguration>> GetAllAsync(CancellationToken cancellationToken)
        {
            await this.CacheDevicesAsync(cancellationToken);

            return this.devices?.Values.AsEnumerable() ?? Enumerable.Empty<DeviceConfiguration>();
        }

        public Task<IEnumerable<IHistoricalValue>?> GetStateHistoryAsync(DeviceContactTarget deviceTarget, DateTime startTimeStamp, DateTime endTimeStamp, CancellationToken cancellationToken) => 
            this.deviceStateManager.Value.GetStateHistoryAsync(deviceTarget, startTimeStamp, endTimeStamp);

        public Task<object?> GetStateAsync(DeviceContactTarget deviceTarget, CancellationToken cancellationToken) => 
            this.deviceStateManager.Value.GetStateAsync(deviceTarget);

        public async Task UpdateDeviceAsync(string deviceIdentifier, DeviceConfiguration deviceConfiguration, CancellationToken cancellationToken)
        {
            await this.CacheDevicesAsync(cancellationToken);

            this.devices?.AddOrSet(deviceIdentifier, deviceConfiguration);
        }

        private async Task CacheDevicesAsync(CancellationToken cancellationToken)
        {
            if (this.devices != null)
                return;

            this.devices = new Dictionary<string, DeviceConfiguration>();
            // TODO: Load when implemented updating devices
            //this.devices = (await this.configurationService.LoadDevicesAsync()).ToDictionary(d => d.Identifier);
        }
    }
}