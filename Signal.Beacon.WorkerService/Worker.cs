using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.MessageQueue;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly IMqttClient mqttClient;
        private readonly Lazy<IEnumerable<IWorkerService>> workerServices;
        private readonly ILogger<Worker> logger;
        
        public Worker(
            IMqttClient mqttClient,
            Lazy<IEnumerable<IWorkerService>> workerServices, 
            ILogger<Worker> logger)
        {
            this.mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            this.workerServices = workerServices ?? throw new ArgumentNullException(nameof(workerServices));
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.mqttClient.StartAsync(stoppingToken);

            foreach (var workerService in this.workerServices.Value)
            {
                try
                {
                    this.logger.LogInformation("Starting {WorkerServiceName}...", workerService.GetType().Name);
                    await workerService.StartAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to start worker service {WorkerServiceName}", workerService.GetType().Name);
                }
            }

            this.logger.LogInformation("All worker services started.");

            while (!stoppingToken.IsCancellationRequested) 
                await Task.Delay(1000, stoppingToken);
        }
    }
}
