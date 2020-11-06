using System;
using System.Threading.Tasks;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application
{
    public class ConditionEvaluatorValueProvider : IConditionEvaluatorValueProvider
    {
        private readonly IDevicesService devicesService;

        public ConditionEvaluatorValueProvider(
            IDevicesService devicesService)
        {
            this.devicesService = devicesService ?? throw new ArgumentNullException(nameof(devicesService));
        }

        public async Task<object?> GetValueAsync(IConditionValue conditionValue)
        {
            return conditionValue switch
            {
                ConditionValueStatic conditionValueStatic => conditionValueStatic.Value,
                ConditionValueDeviceState conditionValueDeviceState => conditionValueDeviceState.Target == null
                    ? null
                    : await this.devicesService.GetStateAsync(conditionValueDeviceState.Target),
                _ => throw new NotSupportedException(
                    $"Not supported condition value comparison: {conditionValue.GetType().FullName}")
            };
        }
    }
}