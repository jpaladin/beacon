using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Api.Dtos
{
    public record ConductDto
    {
        public DeviceTarget Target { get; init; }

        public string Value { get; init; }
    }
}
