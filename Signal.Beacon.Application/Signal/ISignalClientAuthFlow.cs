using Signal.Beacon.Application.Auth;

namespace Signal.Beacon.Application.Signal
{
    public interface ISignalClientAuthFlow
    {
        void AssignToken(AuthToken token);
    }
}
