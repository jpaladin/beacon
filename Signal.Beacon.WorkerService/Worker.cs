using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Signal.Beacon.Application.Auth;
using Signal.Beacon.Application.Auth0;
using Signal.Beacon.Application.Processing;
using Signal.Beacon.Application.Signal;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Signal;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ISignalBeaconClient signalClient;
        private readonly ISignalClientAuthFlow signalClientAuthFlow;
        private readonly Lazy<IEnumerable<IWorkerService>> workerServices;
        private readonly IConfigurationService configurationService;
        private readonly ILogger<Worker> logger;

        public Worker(
            ISignalBeaconClient signalClient,
            ISignalClientAuthFlow signalClientAuthFlow,
            Lazy<IEnumerable<IWorkerService>> workerServices, 
            IConfigurationService configurationService,
            ILogger<Worker> logger)
        {
            this.signalClient = signalClient ?? throw new ArgumentNullException(nameof(signalClient));
            this.signalClientAuthFlow = signalClientAuthFlow ?? throw new ArgumentNullException(nameof(signalClientAuthFlow));
            this.workerServices = workerServices ?? throw new ArgumentNullException(nameof(workerServices));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class BeaconConfiguration
        {
            public string? Identifier { get; set; }

            public AuthToken? Token {get;set;}
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Load configuration
            var config = await this.configurationService.LoadAsync<BeaconConfiguration>("Beacon.json", stoppingToken);
            if (config.Token == null)
            {
                this.logger.LogInformation("Beacon not registered. Started registration...");
                
                try
                {
                    // Assign identifier to Beacon
                    if (string.IsNullOrWhiteSpace(config.Identifier))
                    {
                        config.Identifier = Guid.NewGuid().ToString();
                        await this.configurationService.SaveAsync("beacon.json", config, stoppingToken);
                    }

                    // Authorize Beacon
                    var deviceCodeResponse = await new Auth0DeviceAuthorization().GetDeviceCodeAsync(stoppingToken);
                    this.logger.LogInformation("Device auth: {Response}",
                        JsonConvert.SerializeObject(deviceCodeResponse));
                    
                    // TODO: Post device flow request to user (CTA)
                    
                    var token = await new Auth0DeviceAuthorization().WaitTokenAsync(deviceCodeResponse, stoppingToken);
                    this.logger.LogInformation("Authorized successfully.");

                    // Register Beacon
                    this.signalClientAuthFlow.AssignToken(token);
                    await this.signalClient.RegisterBeaconAsync(config.Identifier, stoppingToken);

                    config.Token = token;
                    await this.configurationService.SaveAsync("beacon.json", config, stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to register Beacon. Some functionality will be limited.");
                }
            }
            else
            {
                this.signalClientAuthFlow.AssignToken(config.Token);
            }
            
            // Start worker services
            await Task.WhenAll(this.workerServices.Value.Select(ws => this.StartWorkerService(ws, stoppingToken)));
            this.logger.LogInformation("All worker services started.");

            // Wait for cancellation token
            while (!stoppingToken.IsCancellationRequested)
                await Task.WhenAny(Task.Delay(-1, stoppingToken));

            // Stop services
            await Task.WhenAll(this.workerServices.Value.Select(this.StopWorkerService));
        }

        private Task StopWorkerService(IWorkerService workerService)
        {
            return Task.Run(async () =>
            {
                try
                {
                    this.logger.LogInformation("Stopping {WorkerServiceName}...", workerService.GetType().Name);
                    await workerService.StopAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Service {WorkerServiceName} stopping timed out.",
                        workerService.GetType().Name);
                }
            });
        }

        private Task StartWorkerService(IWorkerService workerService, CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    this.logger.LogInformation("Starting {WorkerServiceName}...", workerService.GetType().Name);
                    await workerService.StartAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to start worker service {WorkerServiceName}",
                        workerService.GetType().Name);
                }
            }, stoppingToken);
        }
    }
}
