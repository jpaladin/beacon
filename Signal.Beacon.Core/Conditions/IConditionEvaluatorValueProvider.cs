using System.Threading.Tasks;

namespace Signal.Beacon.Core.Conditions
{
    public interface IConditionEvaluatorValueProvider
    {
        Task<object?> GetValueAsync(IConditionValue conditionValue);
    }
}