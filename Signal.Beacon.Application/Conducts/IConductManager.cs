using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Conducts;

namespace Signal.Beacon.Application.Conducts
{
    internal interface IConductManager
    {
        Task StartAsync(CancellationToken cancellationToken);

        IDisposable Subscribe(string channel, Func<Conduct, CancellationToken, Task> handler);

        Task PublishAsync(IEnumerable<Conduct> conducts, CancellationToken cancellationToken);
    }
}