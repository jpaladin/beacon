using System.Collections.Generic;
using System.Linq;

namespace Signal.Beacon.Core.Devices
{
    public class DeviceEndpoint
    {
        public string Channel { get; }

        public IEnumerable<DeviceContact> Contacts { get; }

        public DeviceEndpoint(
            string channel,
            IEnumerable<DeviceContact>? contacts = null)
        {
            this.Channel = channel;
            this.Contacts = contacts ?? Enumerable.Empty<DeviceContact>();
        }
    }
}