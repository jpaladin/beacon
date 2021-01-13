using System.Collections.Generic;

namespace Signal.Beacon.Application.Signal
{
    public class SignalDeviceDto
    {
        public string? Id { get; set; }

        public string? DeviceIdentifier { get; set; }

        public string? Alias { get; set; }

        public IEnumerable<SignalDeviceEndpointDto>? Endpoints { get; set; }

        public string? Manufacturer { get; set; }

        public string? Model { get; set; }
    }
}