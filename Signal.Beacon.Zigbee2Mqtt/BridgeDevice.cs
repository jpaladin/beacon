using Newtonsoft.Json;

namespace Signal.Beacon.Zigbee2Mqtt
{
    internal class BridgeDevice
    {
        [JsonProperty("ieee_address", NullValueHandling = NullValueHandling.Ignore)]
        public string? IeeeAddress { get; set; }

        [JsonProperty("friendly_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? FriendlyName { get; set;  }

        public BridgeDeviceDefinition? Definition { get; set; }
    }
}