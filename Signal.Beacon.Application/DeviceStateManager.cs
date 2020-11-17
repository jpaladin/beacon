using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Extensions;
using Signal.Beacon.Core.MessageQueue;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Application
{
    public class DeviceStateManager : IDeviceStateManager
    {
        private readonly IMqttClient client;

        private readonly Dictionary<DeviceTarget, object?> states = 
            new Dictionary<DeviceTarget, object?>();

        private readonly Dictionary<DeviceTarget, ICollection<IHistoricalValue>> statesHistory =
            new Dictionary<DeviceTarget, ICollection<IHistoricalValue>>();


        public DeviceStateManager(
            IMqttClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public void SetState(DeviceTarget target, object? value)
        {
            var valueString = value?.ToString();
            object? setValue = valueString;
            if (double.TryParse(valueString, out var valueDouble))
                setValue = valueDouble;
            else if (bool.TryParse(valueString, out var valueBool))
                setValue = valueBool;

            this.states.AddOrSet(target, setValue);
            this.statesHistory.Append(target, new HistoricalValue(value, DateTime.UtcNow));

            this.client.PublishAsync(
                $"signal/devices/state/set/{target.Identifier.EscapeSlashes()}/{target.Channel}/{target.Contact}",
                setValue);
        }

        public async Task<IEnumerable<IHistoricalValue>?> GetStateHistoryAsync(
            DeviceTarget target,
            DateTime startTimeStamp, 
            DateTime endTimeStamp) =>
            this.statesHistory.TryGetValue(target, out var history)
                ? history.Where(hv => hv.TimeStamp >= startTimeStamp && hv.TimeStamp <= endTimeStamp)
                : null;

        public async Task<object?> GetStateAsync(DeviceTarget target) => 
            this.states.TryGetValue(target, out var state) ? state : null;
    }
}