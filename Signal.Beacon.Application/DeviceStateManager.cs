using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signal.Beacon.Application.Conducts;
using Signal.Beacon.Application.PubSub;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Extensions;
using Signal.Beacon.Core.Signal;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Application
{
    public class DeviceStateManager : IDeviceStateManager
    {
        private readonly IPubSubHub<DeviceTarget> deviceStateHub;
        private readonly ISignalClient signalClient;
        private readonly ILogger<DeviceStateManager> logger;
        private readonly ConcurrentDictionary<DeviceContactTarget, object?> states = new();
        private readonly ConcurrentDictionary<DeviceContactTarget, ICollection<IHistoricalValue>> statesHistory = new();


        public DeviceStateManager(
            ISignalClient signalClient,
            IPubSubHub<DeviceTarget> deviceStateHub,
            ILogger<DeviceStateManager> logger)
        {
            this.deviceStateHub = deviceStateHub ?? throw new ArgumentNullException(nameof(deviceStateHub));
            this.signalClient = signalClient ?? throw new ArgumentNullException(nameof(signalClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public IDisposable Subscribe(Func<DeviceTarget, CancellationToken, Task> handler) => 
            this.deviceStateHub.Subscribe(this, handler);

        public async Task SetStateAsync(DeviceTarget target, object? value, CancellationToken cancellationToken)
        {
            var setValue = ParseValue(value);

            // TODO: Check if contact trigger is every or on change
            var currentState = ParseValue(await this.GetStateAsync(target));
            if (currentState == null && setValue == null || (currentState?.Equals(setValue) ?? false))
            {
                this.logger.LogDebug(
                    "Device state ignore because it didn't change. {DeviceId} {Contact}: {Value}",
                    target.Identifier, 
                    target.Contact,
                    setValue);
                return;
            }

            var timeStamp = DateTime.UtcNow;
            this.states.AddOrSet(target, setValue);
            this.statesHistory.Append(target, new HistoricalValue(setValue, timeStamp));

            // Publish state changed to local workers
            await this.deviceStateHub.PublishAsync(new[] {target}, cancellationToken);

            // Publish state changed to Signal API
            try
            {
                await this.signalClient.DevicesPublishStateAsync(target, setValue, timeStamp, cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("IDX10223"))
            {
                this.logger.LogWarning("Failed to push device state update to Signal - Token expired.");
            }

            this.logger.LogDebug(
                "Device state updated - {DeviceId} {Contact}: {Value}", 
                target.Identifier, 
                target.Contact,
                setValue);
        }

        private static object? ParseValue(object? value)
        {
            var valueString = value?.ToString();
            object? setValue = valueString;
            if (double.TryParse(valueString, out var valueDouble))
                setValue = valueDouble;
            else if (bool.TryParse(valueString, out var valueBool))
                setValue = valueBool;
            return setValue;
        }

        public Task<IEnumerable<IHistoricalValue>?> GetStateHistoryAsync(
            DeviceContactTarget target,
            DateTime startTimeStamp, 
            DateTime endTimeStamp) =>
            this.statesHistory.TryGetValue(target, out var history)
                // ReSharper disable once ConstantConditionalAccessQualifier
                ? Task.FromResult(history?.Where(hv => hv.TimeStamp >= startTimeStamp && hv.TimeStamp <= endTimeStamp))
                : Task.FromResult<IEnumerable<IHistoricalValue>?>(null);

        public Task<object?> GetStateAsync(DeviceContactTarget target) =>
            this.states.TryGetValue(target, out var state)
                ? Task.FromResult(state)
                : Task.FromResult<object?>(null);
    }
}