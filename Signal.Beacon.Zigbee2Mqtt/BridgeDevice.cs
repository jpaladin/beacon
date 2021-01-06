using System.Text.Json.Serialization;

namespace Signal.Beacon.Zigbee2Mqtt
{
    internal class BridgeDevice
    {
        [JsonPropertyName("ieee_address")]
        public string? IeeeAddress { get; set; }

        [JsonPropertyName("friendly_name")]
        public string? FriendlyName { get; set;  }

        public BridgeDeviceDefinition? Definition { get; set; }
    }
}