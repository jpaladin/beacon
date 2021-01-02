using Microsoft.Extensions.DependencyInjection;
using Signal.Beacon.Core.Extensions;
using Signal.Beacon.Core.Signal;

namespace Signal.Beacon.Application.Signal
{
    public static class SignalExtensions
    {
        public static IServiceCollection AddSignalApi(this IServiceCollection services)
        {
            return services.AddSingleton<ISignalClient, ISignalClientAuthFlow, SignalClient>();
        }
    }
}
