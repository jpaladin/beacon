using System.Collections.Generic;
using System.Linq;
using Signal.Beacon.Core.Conditions;
using Signal.Beacon.Core.Conducts;

namespace Signal.Beacon.Core.Processes
{
    public class Process
    {
        public string Name { get; }

        public Condition Condition { get; }

        public IEnumerable<Conduct> Conducts { get; }

        public Process(string name, Condition condition, params Conduct[] conducts)
        {
            this.Name = name;
            this.Condition = condition;
            this.Conducts = conducts.ToList();
        }
    }
}