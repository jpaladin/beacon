using System;
using Q42.HueApi.Interfaces;

namespace Signal.Beacon.PhilipsHue
{
    public class BridgeConnection
    {
        public BridgeConnection(BridgeConfig config, ILocalHueClient localClient)
        {
            this.Config = config;
            this.LocalClient = localClient;
        }

        public BridgeConfig Config { get; }

        public ILocalHueClient LocalClient { get; private set; }

        public void AssignNewClient(ILocalHueClient client)
        {
            this.LocalClient = client ?? throw new ArgumentNullException(nameof(client));
        }
    }
}