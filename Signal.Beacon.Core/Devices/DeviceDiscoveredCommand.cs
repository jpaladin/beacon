using System;
using System.Collections.Generic;
using System.Linq;
using Signal.Beacon.Core.Architecture;

namespace Signal.Beacon.Core.Devices
{
    public class DeviceDiscoveredCommand : ICommand
    {
        public string Alias { get; }

        public bool IsConfigured { get; }

        public string Identifier { get; }

        public IEnumerable<DeviceEndpoint> Endpoints { get; set; }

        public string? Model { get; set; }

        public string? Manufacturer { get; set; }

        public DeviceDiscoveredCommand(string alias, string identifier, IEnumerable<DeviceEndpoint>? endpoints = null)
        {
            this.Alias = alias ?? throw new ArgumentNullException(nameof(alias));
            this.Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            this.Endpoints = endpoints ?? Enumerable.Empty<DeviceEndpoint>();
            this.IsConfigured = true;
        }
    }
}