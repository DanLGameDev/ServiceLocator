using System;

namespace DGP.ServiceLocator
{
    public interface IServiceProvider
    {
        bool TryGetService(Type serviceType, out object service);
    }
}