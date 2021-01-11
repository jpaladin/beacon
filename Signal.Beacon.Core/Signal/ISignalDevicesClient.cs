using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Core.Signal
{
    public interface ISignalDevicesClient : ISignalFeatureClient
    {
        Task<string> RegisterDeviceAsync(DeviceDiscoveredCommand discoveredDevice, CancellationToken cancellationToken);

        Task DevicesPublishStateAsync(string deviceId, DeviceTarget target, object? setValue, DateTime timeStamp,
            CancellationToken cancellationToken);

        Task<IEnumerable<DeviceConfiguration>> GetDevicesAsync(CancellationToken cancellationToken);
    }
}