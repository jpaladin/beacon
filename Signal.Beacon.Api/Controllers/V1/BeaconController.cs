using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Signal.Beacon.Api.Dtos;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Api.Controllers.V1
{
    [ApiController]
    [Route("[controller]")]
    public class BeaconController : Controller
    {
        private readonly IDevicesService devicesService;
        private readonly IConductService conductService;

        public BeaconController(
            IDevicesService devicesService,
            IConductService conductService)
        {
            this.devicesService = devicesService ?? throw new ArgumentNullException(nameof(devicesService));
            this.conductService = conductService ?? throw new ArgumentNullException(nameof(conductService));
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
        public async Task<IEnumerable<DeviceConfiguration>> GetDevicesAsync() =>
            await this.devicesService.GetAllAsync();

        [HttpGet]
        [Route("device-state-history")]
        public async Task<IEnumerable<IHistoricalValue>?> GetDeviceStateHistoryAsync(string identifier, string contact, DateTime startTimeStamp, DateTime endTimeStamp) => 
            await this.devicesService.GetStateHistoryAsync(new DeviceTarget(identifier, contact), startTimeStamp, endTimeStamp);

        [HttpGet]
        [Route("device-state")]
        public async Task<string?> GetDeviceStateAsync(string identifier, string contact)
        {
            var value = await this.devicesService.GetStateAsync(new DeviceTarget(identifier, contact));
            return value == null ? null : JsonSerializer.Serialize(value, value.GetType());
        }

        [HttpPost]
        [Route("conduct")]
        public async Task PublishConductAsync(ConductDto conduct) =>
            await this.conductService.PublishConductsAsync("zigbee2mqtt", new[] {new Conduct(conduct.Target, conduct.Value)});
    }
}