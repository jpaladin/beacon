using Q42.HueApi.Interfaces;

namespace Signal.Beacon.PhilipsHue
{
    public class BridgeConnection
    {
        public BridgeConfig Config { get; init; }

        public ILocalHueClient? LocalClient { get; set; }
    }
}