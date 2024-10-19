using System;

namespace DGP.ServiceLocator
{
    public class ServiceQuery
    {
        public ServiceSearchMode SearchMode;
        public ServiceAddress Address;
        public Action<ILocatableService> CallbackFn;
    }
}