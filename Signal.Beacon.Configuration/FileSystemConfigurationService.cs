using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Configuration;
using Signal.Beacon.Core.Devices;
using Signal.Beacon.Core.Processes;

namespace Signal.Beacon.Configuration
{
    public class FileSystemConfigurationService : IConfigurationService
    {
        private const string ProcessesConfigPath = "Processes.json";
        private const string DevicesConfigPath = "Devices.json";

        private readonly ILogger<FileSystemConfigurationService> logger;
        private readonly JsonSerializerSettings deserializationSettings;
        private readonly JsonSerializerSettings serializationSettings;

        public FileSystemConfigurationService(ILogger<FileSystemConfigurationService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.serializationSettings = new()
            {
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Populate,
            };
            this.deserializationSettings = new()
            {
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Populate,
                Converters =
                {
                    new BestMatchDeserializeConverter<IConditionValue>(
                        typeof(ConditionValueStatic),
                        typeof(ConditionValueDeviceState)),
                    new BestMatchDeserializeConverter<IConditionComparable>(
                        typeof(ConditionValueComparison),
                        typeof(Condition))
                }
            };

            this.logger.LogDebug("Configuration path: {AbsolutePath}", AsAbsolutePath(""));
        }

        public async Task<IEnumerable<DeviceConfiguration>> LoadDevicesAsync() => 
            await this.LoadFromFileSystemAsync<List<DeviceConfiguration>>(DevicesConfigPath);

        public async Task<IEnumerable<Process>> LoadProcessesAsync() => 
            await this.LoadFromFileSystemAsync<List<Process>>(ProcessesConfigPath);

        private async Task<T> LoadFromFileSystemAsync<T>(string path)
            where T : new()
        {
            var absolutePath = AsAbsolutePath(path);
            if (File.Exists(absolutePath))
                return JsonConvert.DeserializeObject<T>(
                    await File.ReadAllTextAsync(absolutePath), 
                    this.deserializationSettings) ?? new T();
            return new T();
        }

        private static string AsAbsolutePath(string path) =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SignalBeacon",
                "config",
                path);
    }
}