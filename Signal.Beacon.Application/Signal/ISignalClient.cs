using System.Threading;
using System.Threading.Tasks;

namespace Signal.Beacon.Application.Signal
{
    internal interface ISignalClient
    {
        Task PostAsJsonAsync<T>(string url, T data, CancellationToken cancellationToken);
        Task<TResponse?> PostAsJsonAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken cancellationToken, bool renewTokenIfExpired = true);
        Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken);
    }
}