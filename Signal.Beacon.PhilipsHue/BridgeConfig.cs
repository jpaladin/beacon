using System;

namespace Signal.Beacon.PhilipsHue
{
    public class BridgeConfig
    {
        public BridgeConfig(string id, string ipAddress, string localAppKey)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(ipAddress));
            if (string.IsNullOrWhiteSpace(localAppKey))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(localAppKey));

            this.Id = id;
            this.IpAddress = ipAddress;
            this.LocalAppKey = localAppKey;
        }

        public string Id { get; init; }

        public string IpAddress { get; set; }

        public string LocalAppKey { get; set; }
    }
}