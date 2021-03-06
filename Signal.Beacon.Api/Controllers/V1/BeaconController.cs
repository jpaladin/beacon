using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Processes;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Api.Controllers.V1
{
    [ApiController]
    [Route("[controller]")]
    public class BeaconController : Controller
    {
        private readonly IDevicesDao devicesDao;
        private readonly IProcessesDao processesDao;

        public BeaconController(
            IDevicesDao devicesDao,
            IProcessesDao processesDao)
        {
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
            this.processesDao = processesDao ?? throw new ArgumentNullException(nameof(processesDao));
        }
        
        [HttpGet]
        [Route("version")]
        public IActionResult GetVersion() =>
            this.Ok(new
            {
                typeof(BeaconController).Assembly.GetName().Version
            });
        
        [HttpGet]
        [Route("devices")]
        public async Task<IEnumerable<DeviceConfiguration>> GetDevicesAsync(CancellationToken cancellationToken) =>
            await this.devicesDao.GetAllAsync(cancellationToken);

        [HttpGet]
        [Route("device-state-history")]
        public async Task<IEnumerable<IHistoricalValue>?> GetDeviceStateHistoryAsync(string identifier, string contact, DateTime startTimeStamp, DateTime endTimeStamp, CancellationToken cancellationToken) => 
            await this.devicesDao.GetStateHistoryAsync(new DeviceContactTarget(identifier, contact), startTimeStamp, endTimeStamp, cancellationToken);

        [HttpGet]
        [Route("device-state")]
        public async Task<string?> GetDeviceStateAsync(string identifier, string contact, CancellationToken cancellationToken)
        {
            var value = await this.devicesDao.GetStateAsync(new DeviceContactTarget(identifier, contact), cancellationToken);
            return value == null ? null : JsonSerializer.Serialize(value, value.GetType());
        }

        [HttpGet]
        [Route("processes")]
        public async Task<IEnumerable<Process>> GetProcessesAsync(CancellationToken cancellationToken) => 
            await this.processesDao.GetAllAsync(cancellationToken);
    }
}