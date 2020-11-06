using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Core.Conducts
{
    public class Conduct
    {
        public DeviceTarget Target { get; }

        public object Value { get; }

        public Conduct(DeviceTarget target, object value)
        {
            this.Target = target;
            this.Value = value;
        }
    }
}