using System;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Application
{
    public class ConditionEvaluatorValueProvider : IConditionEvaluatorValueProvider
    {
        private readonly IDevicesDao devicesDao;

        public ConditionEvaluatorValueProvider(
            IDevicesDao devicesDao)
        {
            this.devicesDao = devicesDao ?? throw new ArgumentNullException(nameof(devicesDao));
        }

        public async Task<object?> GetValueAsync(IConditionValue conditionValue, CancellationToken cancellationToken)
        {
            return conditionValue switch
            {
                ConditionValueStatic conditionValueStatic => conditionValueStatic.Value,
                ConditionValueDeviceState conditionValueDeviceState => conditionValueDeviceState.Target == null
                    ? null
                    : await this.devicesDao.GetStateAsync(conditionValueDeviceState.Target, cancellationToken),
                _ => throw new NotSupportedException(
                    $"Not supported condition value comparison: {conditionValue.GetType().FullName}")
            };
        }
    }
}