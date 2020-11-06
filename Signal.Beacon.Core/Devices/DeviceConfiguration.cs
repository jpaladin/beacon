using System.Collections.Generic;
using System.Linq;

namespace Signal.Beacon.Core.Devices
{
    public class DeviceConfiguration
    {
        public string Alias { get; }

        public string Identifier { get; }

        public IEnumerable<DeviceEndpoint> Endpoints { get; }

        public DeviceConfiguration(string alias, string identifier, IEnumerable<DeviceEndpoint>? endpoints = null)
        {
            this.Alias = alias;
            this.Identifier = identifier;
            this.Endpoints = endpoints ?? Enumerable.Empty<DeviceEndpoint>();
        }
    }
}