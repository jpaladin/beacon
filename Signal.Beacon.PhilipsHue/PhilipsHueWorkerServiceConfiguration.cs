using System.Collections.Generic;

namespace Signal.Beacon.PhilipsHue
{
    internal class PhilipsHueWorkerServiceConfiguration
    {
        public List<BridgeConfig> Bridges { get; set; } = new();
    }
}