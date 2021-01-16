using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Application.Signal.SignalR
{
    public interface ISignalSignalRConductsHubClient : ISignalSignalRHubClient
    {
        Task OnConductRequestAsync(Func<ConductRequestDto, CancellationToken, Task> handler, CancellationToken cancellationToken);
    }
}