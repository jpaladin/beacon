using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application
{
    public class DevicesCommandHandler : IDevicesCommandHandler
    {
        private readonly IDevicesDao devicesDao;
        private readonly IDeviceStateManager deviceStateManager;
        private readonly ILogger<DevicesCommandHandler> logger;

        public DevicesCommandHandler(
            IDevicesDao devicesDao,
            IDeviceStateManager deviceStateManager,
            ILogger<DevicesCommandHandler> logger)
        {
            this.devicesDao = devicesDao;
            this.deviceStateManager = deviceStateManager ?? throw new ArgumentNullException(nameof(deviceStateManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task HandleAsync(DeviceStateSetCommand command)
        {
            this.deviceStateManager.SetState(command.Target, command.Value);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(DeviceDiscoveredCommand command)
        {
            this.logger.LogInformation(
                "New device discovered: {DeviceAlias} ({DeviceIdentifier})",
                command.Device.Alias, command.Device.Identifier);

            await this.devicesDao.UpdateDeviceAsync(command.Device.Identifier, command.Device);
        }
    }
}