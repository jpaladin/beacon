using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Application.Signal.SignalR
{
    public interface ISignalSignalRDevicesHubClient : ISignalSignalRHubClient
    {
        Task OnDeviceStateAsync(Func<SignalDeviceStatePublishDto, CancellationToken, Task> handler, CancellationToken cancellationToken);
    }
}