using System.Text.Json.Serialization;

namespace Signal.Beacon.Channel.Signal
{
    public class SignalSensorTelemetry
    {
        [JsonPropertyName("locked")]
        public bool Locked { get; set; }
    }
}