using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.PhilipsHue
{
    public class PhilipsHueWorkerService : IWorkerService
    {
        private readonly ILogger<PhilipsHueWorkerService> logger;

        public PhilipsHueWorkerService(
            ILogger<PhilipsHueWorkerService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //this.logger.LogDebug("Scanning for bridge...");

            //var bridges = await HueBridgeDiscovery.CompleteDiscoveryAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
            

            //this.logger.LogInformation("Bridges found: {BridgesCount}", bridges.Count);

            //if (bridges.Count > 0)
            //{
            //    ILocalHueClient client = new LocalHueClient(bridges.First().IpAddress);
            //    while (true)
            //    {
            //        try
            //        {
            //            await Task.Delay(5000, cancellationToken);
            //            var appKey = await client.RegisterAsync("Signal.Beacon.Hue", "HueBeacon");
            //            if (appKey == null)
            //                throw new Exception("Hub responded with null key.");

            //            this.logger.LogInformation("AppKey: {AppKey}", appKey);
            //            client.Initialize(appKey);
            //        }
            //        catch (LinkButtonNotPressedException ex)
            //        {
            //            this.logger.LogTrace(ex, "Bridge not connected. Waiting for user button press.");
            //            this.logger.LogInformation("Press button on Philips Hue bridge to connect...");
            //        }
            //    }
            //}
        }
    }
}
