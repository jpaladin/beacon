namespace Signal.Beacon.PhilipsHue
{
    internal class PhilipsHueLight
    {
        public PhilipsHueLight(string uniqueId, string onBridgeId, string bridgeId)
        {
            this.UniqueId = uniqueId;
            this.OnBridgeId = onBridgeId;
            this.BridgeId = bridgeId;
        }

        public string UniqueId { get; }

        public string OnBridgeId { get; }

        public string BridgeId { get; }
    }
}