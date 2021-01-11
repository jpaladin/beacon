using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Extensions;
using Signal.Beacon.Core.Signal;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Application
{
    public class DevicesDao : IDevicesDao
    {
        private readonly ISignalDevicesClient devicesClient;
        private readonly Lazy<IDeviceStateManager> deviceStateManager;
        private readonly ILogger<DevicesDao> logger;
        private Dictionary<string, DeviceConfiguration>? devices;
        private readonly object cacheLock = new();
        private Task<IEnumerable<DeviceConfiguration>>? getDevicesTask;

        public DevicesDao(
            ISignalDevicesClient devicesClient,
            Lazy<IDeviceStateManager> deviceStateManager,
            ILogger<DevicesDao> logger)
        {
            this.devicesClient = devicesClient ?? throw new ArgumentNullException(nameof(devicesClient));
            this.deviceStateManager = deviceStateManager ?? throw new ArgumentNullException(nameof(deviceStateManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        public async Task UpdateDeviceAsync(string identifier, DeviceConfiguration deviceConfiguration, CancellationToken cancellationToken)
        {
            await this.CacheDevicesAsync(cancellationToken);

            this.devices?.AddOrSet(identifier, deviceConfiguration);
        }

        private async Task CacheDevicesAsync(CancellationToken cancellationToken)
        {
            if (this.devices != null)
                return;

            try
            {
                this.getDevicesTask ??= this.devicesClient.GetDevicesAsync(cancellationToken);

                var remoteDevices = (await this.getDevicesTask).ToList();

                lock (this.cacheLock)
                {
                    if (this.devices != null)
                        return;

                    try
                    {
                        this.devices = new Dictionary<string, DeviceConfiguration>();
                        foreach (var deviceConfiguration in remoteDevices)
                            this.devices.Add(deviceConfiguration.Identifier, deviceConfiguration);
                    }
                    finally
                    {
                        this.getDevicesTask = null;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogDebug(ex, "Failed to cache devices.");
                throw;
            }
        }
    }
}