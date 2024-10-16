using System;

namespace DGP.ServiceLocator
{
    internal class ServiceQuery
    {
        public ServiceSearchMode SearchMode;
        public ServiceAddress Address;
        public Action<ILocatableService> CallbackFn;
    }
}