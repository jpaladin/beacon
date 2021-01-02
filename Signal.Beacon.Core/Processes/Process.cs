using System.Collections.Generic;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Conducts;
using Signal.Beacon.Core.Devices;

namespace Signal.Beacon.Core.Processes
{
    public class Process
    {
        public string Name { get; set; }

        public IEnumerable<DeviceTarget> Triggers { get; set; }

        public Condition Condition { get; set; }

        public IEnumerable<Conduct> Conducts { get; set; }
    }
}