using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Extensions;
using Signal.Beacon.Core.MessageQueue;

namespace Signal.Beacon.Application
{
    public class DeviceStateManager : IDeviceStateManager
    {
        private readonly IMqttClient client;

        private readonly Dictionary<DeviceTarget, object?> states = 
            new Dictionary<DeviceTarget, object?>();


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

            if (!this.states.ContainsKey(target))
                this.states.Add(target, null);
            this.states[target] = setValue;

            this.client.PublishAsync(
                $"signal/devices/state/set/{target.Identifier.EscapeSlashes()}/{target.Channel}/{target.Contact}",
                setValue);
        }

        public async Task<object?> GetStateAsync(DeviceTarget target) => 
            this.states.TryGetValue(target, out var state) ? state : null;
    }
}