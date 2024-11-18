using System;

namespace DGP.ServiceLocator
{
    public class ServiceQuery
    {
        public ServiceSearchMode SearchMode;
        public Type SearchedType;
        public Action<object> CallbackFn;
    }
}