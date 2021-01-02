using Microsoft.Extensions.DependencyInjection;
using Signal.Beacon.Application.Conducts;
using Signal.Beacon.Application.Network;
using Signal.Beacon.Application.Processing;
using Signal.Beacon.Application.PubSub;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Network;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddBeaconApplication(this IServiceCollection services)
        {
            services.AddTransient<IConditionEvaluatorService, ConditionEvaluatorService>();
            services.AddSingleton<IConditionEvaluatorValueProvider, ConditionEvaluatorValueProvider>();
            services.AddTransient<ICommandHandler<DeviceStateSetCommand>, DevicesCommandHandler>();
            services.AddTransient<ICommandHandler<DeviceDiscoveredCommand>, DevicesCommandHandler>();
            services.AddTransient<IProcessesService, ProcessesService>();
            services.AddSingleton<IDevicesDao, DevicesDao>();
            services.AddSingleton<IProcessesDao, ProcessesDao>();
            services.AddSingleton<IProcessesRepository, ProcessesRepository>();
            services.AddSingleton<IDeviceStateManager, DeviceStateManager>();

            // PubSub
            services.AddTransient<IPubSubHub<DeviceTarget>, PubSubHub<DeviceTarget>>();
            services.AddTransient<IPubSubTopicHub<Conduct>, PubSubTopicHub<Conduct>>();

            // Processing
            services.AddSingleton<IProcessor, Processor>();

            // Conducts
            services.AddSingleton<IConductManager, ConductManager>();
            services.AddTransient<IConductSubscriberClient, ConductSubscriberClient>();

            // Network
            services.AddTransient<IHostInfoService, HostInfoService>();

            return services;
        }
    }
}