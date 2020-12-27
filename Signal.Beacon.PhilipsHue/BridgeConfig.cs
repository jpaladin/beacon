namespace Signal.Beacon.PhilipsHue
{
    public class BridgeConfig
    {
        public string Id { get; init; }

        public string? IpAddress { get; set; }

        public string? LocalAppKey { get; set; }
    }
}