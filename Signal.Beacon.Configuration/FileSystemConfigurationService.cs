using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Configuration;

namespace Signal.Beacon.Configuration
{
    public class FileSystemConfigurationService : IConfigurationService
    {
        private readonly ILogger<FileSystemConfigurationService> logger;
        private readonly JsonSerializerSettings deserializationSettings;
        private readonly JsonSerializerSettings serializationSettings;

        public FileSystemConfigurationService(ILogger<FileSystemConfigurationService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.serializationSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Populate,
            };
            this.deserializationSettings = new JsonSerializerSettings
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

        public async Task<T> LoadAsync<T>(string name, CancellationToken cancellationToken) where T : new() =>
            await this.LoadFromFileSystemAsync<T>(name, cancellationToken);

        public async Task SaveAsync<T>(string name, T config, CancellationToken cancellationToken) =>
            await this.SaveToFileSystemAsync(name, config, cancellationToken);

        private async Task SaveToFileSystemAsync<T>(string path, T config, CancellationToken cancellationToken)
        {
            var absolutePath = AsAbsolutePath(path);
            
            // Create directory if applicable
            if (Path.GetDirectoryName(absolutePath) is { } absolutePathDirectory)
                Directory.CreateDirectory(absolutePathDirectory);

            await File.WriteAllTextAsync(absolutePath,
                JsonConvert.SerializeObject(config, this.serializationSettings), cancellationToken);
        }

        private async Task<T> LoadFromFileSystemAsync<T>(string path, CancellationToken cancellationToken)
            where T : new()
        {
            var absolutePath = AsAbsolutePath(path);
            if (File.Exists(absolutePath))
                return JsonConvert.DeserializeObject<T>(
                    await File.ReadAllTextAsync(absolutePath, cancellationToken), 
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