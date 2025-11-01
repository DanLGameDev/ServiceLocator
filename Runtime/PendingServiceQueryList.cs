using System;
using System.Collections.Generic;

namespace DGP.ServiceLocator
{
    internal class PendingServiceQueryList : IDisposable
    {
        private readonly List<ServiceQuery> _queries;
        private readonly Func<Type, ServiceSearchMode, object> _locator;

        public PendingServiceQueryList(Func<Type, ServiceSearchMode, object> locator, int capacity = 8)
        {
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _queries = new List<ServiceQuery>(capacity);
        }

        public void Add(ServiceQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
                
            _queries.Add(query);
        }

        public void TryResolvePendingQueries()
        {
            for (var index = _queries.Count - 1; index >= 0; index--)
            {
                var query = _queries[index];
                var result = _locator(query.SearchedType, query.SearchMode);

                if (query.TryFulfill(result))
                    _queries.RemoveAt(index);
            }
        }

        public void Clear() => _queries.Clear();
        public void Dispose() => _queries.Clear();
    }
}