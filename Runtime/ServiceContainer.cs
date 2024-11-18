using System;
using System.Collections.Generic;
using DGP.ServiceLocator.Injectable;

namespace DGP.ServiceLocator
{
    public class ServiceContainer
    {
        public event Action OnServicesListChanged;

        public readonly ServiceContainer ParentContainer;
        public readonly Dictionary<Type, ILocatableService> RegisteredServices = new();
        public readonly List<ServiceQuery> PendingServiceQueries = new List<ServiceQuery>(capacity: 8);

        private readonly Lazy<ServiceInjector> _injector;
        public ServiceInjector Injector => _injector.Value;

        public ServiceContainer()
        {
            _injector = new Lazy<ServiceInjector>(() => new ServiceInjector(this));
        }

        public ServiceContainer(ServiceContainer parentContainer)
        {
            ParentContainer = parentContainer;
            _injector = new Lazy<ServiceInjector>(() => new ServiceInjector(this));

            if (ParentContainer != null)
                ParentContainer.OnServicesListChanged += HandleServiceTreeChanged;
        }

        ~ServiceContainer()
        {
            if (ParentContainer != null)
                ParentContainer.OnServicesListChanged -= HandleServiceTreeChanged;
            
            GC.SuppressFinalize(this);
        }

        private void HandleServiceTreeChanged() => FlushPendingServiceQueries();

        #region Registration

        /// <summary>
        /// Registers a service with the ServiceLocator
        /// </summary>
        /// <param name="service">The service to register, must implement ILocatableService</param>
        /// <typeparam name="TLocatableService">The type of service, must implement ILocatableService</typeparam>
        public void RegisterService<TLocatableService>(TLocatableService service) where TLocatableService : class, ILocatableService
        {
            RegisterService(typeof(TLocatableService), service);
        }

        public void RegisterService(Type type, ILocatableService service)
        {
            RegisteredServices[type] = service;

            OnServicesListChanged?.Invoke();
            FlushPendingServiceQueries();
        }

        /// <summary>
        /// Deregisters a service from the ServiceLocator
        /// </summary>
        /// <typeparam name="TLocatableService">The type of service to deregister</typeparam>
        public void DeregisterService<TLocatableService>() where TLocatableService : class, ILocatableService
        {
            RegisteredServices.Remove(typeof(TLocatableService));
            OnServicesListChanged?.Invoke();
        }

        /// <summary>
        /// De-registers a service from the ServiceLocator
        /// </summary>
        /// <param name="type">The type to deregister</param>
        /// <exception cref="ArgumentNullException">thrown if type is null</exception>
        /// <exception cref="ArgumentException">thrown if type does not implement ILocatableService</exception>
        public void DeregisterService(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(ILocatableService).IsAssignableFrom(type))
                throw new ArgumentException("Type must implement ILocatableService", nameof(type));

            RegisteredServices.Remove(type);
            OnServicesListChanged?.Invoke();
        }

        #endregion

        #region Locate

        /// <summary>
        /// Locates a service and invokes a callback. If the service is not found, the callback will be
        /// invoked when the service is registered.
        /// </summary>
        /// <param name="callback">The callback to invoke when the reference is available</param>
        /// <param name="context">An optional context the service must exist in</param>
        /// <param name="searchMode">The search mode to use when locating services</param>
        /// <typeparam name="TLocatableService">The type of service</typeparam>
        public void LocateServiceAsync<TLocatableService>(Action<TLocatableService> callback, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService
        {
            LocateServiceAsync(typeof(TLocatableService), service => callback((TLocatableService)service), searchMode);
        }

        public void LocateServiceAsync(Type type, Action<ILocatableService> callback, object context = null, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst)
        {
            var result = LocateServiceInternal(type, searchMode);

            if (result != null) {
                callback((ILocatableService)result);
                return;
            }

            PendingServiceQueries.Add(new ServiceQuery
            {
                SearchMode = searchMode,
                SearchedType = type,
                CallbackFn = callback
            });
        }

        private void FlushPendingServiceQueries()
        {
            for (int index = PendingServiceQueries.Count - 1; index >= 0; index--) {
                var query = PendingServiceQueries[index];

                var result = LocateServiceInternal(query.SearchedType, query.SearchMode);

                if (result != null) {
                    query.CallbackFn((ILocatableService)result);
                    PendingServiceQueries.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Locates a service synchronously and returns it. If the service is not found, null is returned.
        /// </summary>
        /// <param name="context">An optional context the service must exist in</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <typeparam name="TLocatableService">The type of service to locate</typeparam>
        /// <returns>Returns the service if located, throws an exception if the service is not registered</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not found</exception>
        public TLocatableService GetService<TLocatableService>(ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService
        {
            var result = LocateServiceInternal(typeof(TLocatableService), searchMode);

            if (result is TLocatableService service)
                return service;

            throw new InvalidOperationException($"Service of type {typeof(TLocatableService).Name} not found");
        }

        /// <summary>
        /// Tries to locate a service and returns true if the service is found.
        /// </summary>
        /// <param name="service">The service if found or null if not</param>
        /// <param name="context">The context the service must exist in</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <typeparam name="TLocatableService">The type of service to locate</typeparam>
        /// <returns></returns>
        public bool TryLocateService<TLocatableService>(out TLocatableService service, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService
        {
            service = LocateServiceInternal(typeof(TLocatableService), searchMode) as TLocatableService;
            return service != null;
        }

        /// <summary>
        /// Tries to locate a service and returns true if the service is found.
        /// </summary>
        /// <param name="type">The type of service to locate</param>
        /// <param name="service">The service if found or null if not</param>
        /// <param name="context">The context the service must exist in</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <returns></returns>
        public bool TryLocateService(Type type, out ILocatableService service, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst)
        {
            service = LocateServiceInternal(type, searchMode) as ILocatableService;
            return service != null;
        }

        private object LocateServiceInternal(Type type, ServiceSearchMode searchMode)
        {
            return searchMode switch
            {
                ServiceSearchMode.LocalOnly => RegisteredServices.GetValueOrDefault(type),
                ServiceSearchMode.GlobalFirst => ParentContainer != null && ParentContainer.TryLocateService(type, out var service, searchMode) ? service : LocateServiceInternal(type, ServiceSearchMode.LocalOnly),
                ServiceSearchMode.LocalFirst => RegisteredServices.GetValueOrDefault(type) ?? ParentContainer?.LocateServiceInternal(type, searchMode),
                _ => null
            };
        }

        #endregion

        /// <summary>
        /// Clears all services and pending service queries
        /// </summary>
        public void ClearServices()
        {
            RegisteredServices.Clear();
            PendingServiceQueries.Clear();

            Injector?.ClearInjectors();
        }
    }
}