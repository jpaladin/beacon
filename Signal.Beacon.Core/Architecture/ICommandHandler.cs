using System.Threading.Tasks;

namespace Signal.Beacon.Core.Devices
{
    public interface ICommandHandler<in T> where T : ICommand
    {
        Task HandleAsync(T command);
    }
}