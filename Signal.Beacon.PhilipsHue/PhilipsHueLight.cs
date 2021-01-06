namespace Signal.Beacon.PhilipsHue
{
    internal class PhilipsHueLight
    {
        public PhilipsHueLight(string uniqueId, string onBridgeId, string bridgeId, PhilipsHueLightState state)
        {
            this.UniqueId = uniqueId;
            this.OnBridgeId = onBridgeId;
            this.BridgeId = bridgeId;
            this.State = state;
        }

        public string UniqueId { get; }

        public string OnBridgeId { get; }

        public string BridgeId { get; }

        public PhilipsHueLightState State { get; }

        public record PhilipsHueLightState(bool On);
    }
}