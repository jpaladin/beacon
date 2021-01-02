using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Core.Signal
{
    public interface ISignalVoiceClient
    {
        Task<byte[]> GetTextAudioAsync(string text, CancellationToken cancellationToken);
    }

    public interface ISignalClient
    {
        Task RegisterBeaconAsync(
            string beaconId, 
            CancellationToken cancellationToken);

        Task DevicesPublishStateAsync(DeviceTarget target, object? setValue, DateTime timeStamp,
            CancellationToken cancellationToken);
    }
}
