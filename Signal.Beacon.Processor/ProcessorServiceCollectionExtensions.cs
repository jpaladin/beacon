using Microsoft.Extensions.DependencyInjection;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.Processor
{
    public static class ProcessorServiceCollectionExtensions
    {
        public static IServiceCollection AddBeaconProcessor(this IServiceCollection services)
        {
            return services.AddSingleton<IWorkerService, Processor>();
        }
    }
}