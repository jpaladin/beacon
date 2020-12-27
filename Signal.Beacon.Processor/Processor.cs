using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Processor
{
    internal class Processor : IProcessor
    {
        private readonly ICommandHandler<DeviceStateSetCommand> deviceStateSetCommandHandler;
        private readonly IConditionEvaluatorService conditionEvaluatorService;
        private readonly IProcessesService processesService;
        private readonly IConductService conductService;
        private readonly ILogger<Processor> logger;

        public Processor(
            ICommandHandler<DeviceStateSetCommand> deviceStateSetCommandHandler,
            IConditionEvaluatorService conditionEvaluatorService,
            IProcessesService processesService,
            IConductService conductService,
            ILogger<Processor> logger)
        {
            this.deviceStateSetCommandHandler = deviceStateSetCommandHandler ?? throw new ArgumentNullException(nameof(deviceStateSetCommandHandler));
            this.conditionEvaluatorService = conditionEvaluatorService ?? throw new ArgumentNullException(nameof(conditionEvaluatorService));
            this.processesService = processesService ?? throw new ArgumentNullException(nameof(processesService));
            this.conductService = conductService ?? throw new ArgumentNullException(nameof(conductService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // TODO: Subscribe to state changes
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        //private async Task HandlerRooms(MqttMessage arg)
        //{
        //    var conduct = JsonConvert.DeserializeObject<Conduct>(arg.Payload);
        //    await this.deviceStateSetCommandHandler.HandleAsync(new DeviceStateSetCommand(conduct.Target, conduct.Value));
        //}

        //private async Task Handler(MqttMessage arg)
        //{
        //    var deviceTarget = arg.Topic.Replace("signal/devices/state/set/", "");
        //    var deviceTargetSplit = deviceTarget.Split("/", StringSplitOptions.RemoveEmptyEntries);

        //    this.logger.LogDebug("Got device state changes. Processing...");

        //    await this.ProcessStateChangedAsync(new DeviceTarget(
        //        deviceTargetSplit[0].UnescapeSlashes(), 
        //        deviceTargetSplit[2].UnescapeSlashes(),
        //        deviceTargetSplit[1].UnescapeSlashes()));
        //}

        private async Task ProcessStateChangedAsync(DeviceTarget target, CancellationToken cancellationToken)
        {
            var availableTriggers =
                (await this.processesService.GetAllAsync(cancellationToken))
                .Select(p => new
                {
                    Process = p,
                    Triggers = p.Condition.Operations
                        .OfType<ConditionValueComparison>()
                        .SelectMany(co =>
                            new[] {co.Left as ConditionValueDeviceState, co.Right as ConditionValueDeviceState})
                        .Where(t => t != null)
                        .Select(t => t!)
                })
                .Where(s => s.Triggers.Any())
                .ToList();

            var triggers = availableTriggers.Where(t => t.Triggers.Any(tt => tt.Target == target)).ToList();
            if (!triggers.Any())
            {
                this.logger.LogTrace("Change on target {DeviceEndpointTarget} ignored.", target);
                return;
            }

            // Execute triggers that meet conditions
            foreach (var trigger in triggers)
            {
                var result = false;
                try
                {
                    result = await this.conditionEvaluatorService.IsConditionMetAsync(trigger.Process.Condition, cancellationToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Process condition invalid. Recheck your configuration. ProcessName: {ProcessName}", trigger.Process.Name);
                }

                if (!result) 
                    continue;

                this.logger.LogInformation("Executing \"{ProcessName}\"... (trigger {Target})", trigger.Process.Name, target);

                // Publish conduct
                await this.conductService.PublishConductsAsync(trigger.Process.Conducts, cancellationToken);
            }
        }
    }
}