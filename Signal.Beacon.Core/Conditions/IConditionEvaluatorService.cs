using System.Threading.Tasks;

namespace Signal.Beacon.Core.Conditions
{
    public interface IConditionEvaluatorService
    {
        Task<bool> IsConditionMetAsync(IConditionComparable comparable);
    }
}