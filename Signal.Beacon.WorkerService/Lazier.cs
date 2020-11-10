using System;
using Microsoft.Extensions.DependencyInjection;

namespace Signal.Beacon.WorkerService
{
    internal class Lazier<T> : Lazy<T> where T : class
    {
        public Lazier(IServiceProvider provider)
            : base(provider.GetRequiredService<T>)
        {
        }
    }
}