using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Signal.Beacon.Api;
using Signal.Beacon.Application;
using Signal.Beacon.Configuration;
using Signal.Beacon.PhilipsHue;
using Signal.Beacon.Processor;
using Signal.Beacon.Zigbee2Mqtt;

namespace Signal.Beacon.WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(opts =>
                    {
                        opts.Listen(IPAddress.Loopback, 5002);
                        opts.ListenAnyIP(5003);
                        opts.ListenAnyIP(5004, ko => ko.UseHttps());
                        opts.ListenLocalhost(5005);
                        opts.ListenLocalhost(5006, ko => ko.UseHttps());
                    });

                    webBuilder.ConfigureServices(services =>
                    {
                        services
                            .AddHostedService<Worker>()
                            .AddBeaconConfiguration()
                            .AddBeaconApplication()
                            .AddBeaconProcessor()
                            .AddZigbee2Mqtt()
                            .AddPhilipsHue();

                        services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
                    });
                });
    }
}
