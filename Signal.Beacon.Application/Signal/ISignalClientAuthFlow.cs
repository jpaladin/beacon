using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Application.Auth;

namespace Signal.Beacon.Application.Signal
{
    public interface ISignalClientAuthFlow
    {
        void AssignToken(AuthToken token);

        Task<AuthToken?> GetTokenAsync(CancellationToken cancellationToken);
    }
}
