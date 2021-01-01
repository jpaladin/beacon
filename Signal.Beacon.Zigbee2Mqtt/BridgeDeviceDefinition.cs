using System.Collections.Generic;

namespace Signal.Beacon.Zigbee2Mqtt
{
    internal class BridgeDeviceDefinition
    {
        public string? Model { get; set; }

        public string? Vendor { get; set; }

        public List<BridgeDeviceExposeFeature>? Exposes { get; set; }
    }
}