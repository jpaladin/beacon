using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
                        opts.ListenAnyIP(5000, lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
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
