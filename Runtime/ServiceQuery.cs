using System;

namespace DGP.ServiceLocator
{
    public class ServiceQuery
    {
        public ServiceSearchMode SearchMode { get; }
        public Type SearchedType { get; }
        
        private readonly Action<object> _callback;
        private bool _isFulfilled;

        public bool IsFulfilled => _isFulfilled;

        public ServiceQuery(Type searchedType, ServiceSearchMode searchMode, Action<object> callback)
        {
            SearchedType = searchedType ?? throw new ArgumentNullException(nameof(searchedType));
            SearchMode = searchMode;
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _isFulfilled = false;
        }

        public bool TryFulfill(object service)
        {
            if (_isFulfilled)
                return false;

            if (service == null)
                return false;

            _isFulfilled = true;
            _callback(service);
            return true;
        }
    }
}