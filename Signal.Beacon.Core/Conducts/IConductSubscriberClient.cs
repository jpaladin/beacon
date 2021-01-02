using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Core.Conducts
{
    public interface IConductSubscriberClient
    {
        void Subscribe(string channel, Func<Conduct, CancellationToken, Task> handler);
    }
}