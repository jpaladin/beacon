using System.Collections.Generic;
using System.Linq;

namespace Signal.Beacon.Core.Devices
{
    public class DeviceEndpoint
    {
        public string Channel { get; }

        public IEnumerable<DeviceContact> Inputs { get; }

        public IEnumerable<DeviceContact> Outputs { get; }

        public DeviceEndpoint(
            string channel,
            IEnumerable<DeviceContact>? inputs = null,
            IEnumerable<DeviceContact>? outputs = null)
        {
            this.Channel = channel;
            this.Inputs = inputs ?? Enumerable.Empty<DeviceContact>();
            this.Outputs = outputs ?? Enumerable.Empty<DeviceContact>();
        }
    }
}