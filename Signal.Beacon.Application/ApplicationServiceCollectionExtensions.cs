using Microsoft.Extensions.DependencyInjection;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.MessageQueue;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddBeaconApplication(this IServiceCollection services)
        {
            services.AddTransient<IConditionEvaluatorService, ConditionEvaluatorService>();
            services.AddSingleton<IConditionEvaluatorValueProvider, ConditionEvaluatorValueProvider>();
            services.AddTransient<IDevicesService, DevicesService>();
            services.AddTransient<IProcessesService, ProcessesService>();
            services.AddSingleton<IDevicesRepository, DevicesRepository>();
            services.AddSingleton<IProcessesRepository, ProcessesRepository>();
            services.AddSingleton<IMqttClient, MqttClient>();
            services.AddSingleton<IDeviceStateManager, DeviceStateManager>();

            return services;
        }
    }
}