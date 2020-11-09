using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Api.Controllers.V1
{
    [ApiController]
    [Route("[controller]")]
    public class BeaconController : Controller
    {
        private readonly IDevicesService devicesService;

        public BeaconController(
            IDevicesService devicesService)
        {
            this.devicesService = devicesService ?? throw new ArgumentNullException(nameof(devicesService));
        }
        
        [HttpGet]
        [Route("version")]
        public IActionResult GetVersion() =>
            this.Ok(new
            {
                Version = typeof(BeaconController).Assembly.GetName().Version
            });
        
        [HttpGet]
        [Route("devices")]
        public async Task<IActionResult> GetDevicesAsync() =>
            this.Ok(await devicesService.GetAllAsync());
    }
}