using System;
using System.Collections;
using System.Collections.Generic;
using DGP.ServiceLocator.Injectable;

namespace DGP.ServiceLocator
{
    public class ServiceContainer : IServiceProvider, IEnumerable<object>
    {
        public event Action OnServicesListChanged;

        private readonly ServiceContainer _parentContainer;
        private readonly PendingServiceQueryList _pendingQueries;
        
        public readonly Dictionary<Type, object> RegisteredServices = new();

        private readonly Lazy<ServiceInjector> _injector;
        public ServiceInjector Injector => _injector.Value;

        public ServiceContainer()
        {
            _injector = new Lazy<ServiceInjector>(() => new ServiceInjector(this));
            _pendingQueries = new PendingServiceQueryList(LocateServiceInternal);
        }

        public ServiceContainer(ServiceContainer parentContainer) : this()
        {
            _parentContainer = parentContainer;
            
            if (_parentContainer != null)
                _parentContainer.OnServicesListChanged += HandleServiceTreeChanged;
        }


        public IEnumerator<object> GetEnumerator()
        {
            foreach (var service in RegisteredServices.Values)
                yield return service;
        }

        ~ServiceContainer()
        {
            if (_parentContainer != null)
                _parentContainer.OnServicesListChanged -= HandleServiceTreeChanged;
            
            GC.SuppressFinalize(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void HandleServiceTreeChanged() => _pendingQueries.TryResolvePendingQueries();

        #region Registration

        /// <summary>
        /// Registers a service with the ServiceLocator
        /// </summary>
        /// <param name="service">The service to register, must implement ILocatableService</param>
        /// <typeparam name="TLocatableService">The type of service, must implement ILocatableService</typeparam>
        public TLocatableService RegisterService<TLocatableService>(TLocatableService service) where TLocatableService : class
        {
            RegisterService(typeof(TLocatableService), service);
            return service;
        }

        public object RegisterService(Type type, object service)
        {
            RegisteredServices[type] = service;

            OnServicesListChanged?.Invoke();
            _pendingQueries.TryResolvePendingQueries();

            return service;
        }

        /// <summary>
        /// Deregisters a service from the ServiceLocator
        /// </summary>
        /// <typeparam name="TLocatableService">The type of service to deregister</typeparam>
        public void DeregisterService<TLocatableService>() where TLocatableService : class
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
        /// <param name="searchMode">The search mode to use when locating services</param>
        /// <typeparam name="TLocatableService">The type of service</typeparam>
        public void LocateServiceAsync<TLocatableService>(Action<TLocatableService> callback, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) where TLocatableService : class
        {
            LocateServiceAsync(typeof(TLocatableService), service => callback((TLocatableService)service), searchMode);
        }

        public void LocateServiceAsync(Type type, Action<object> callback, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst)
        {
            var result = LocateServiceInternal(type, searchMode);

            if (result != null) {
                callback(result);
                return;
            }

            _pendingQueries.Add(new ServiceQuery(type, searchMode, callback));
        }
        
        /// <summary>
        /// Locates a service synchronously and returns it. If the service is not found, null is returned.
        /// </summary>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <typeparam name="TLocatableService">The type of service to locate</typeparam>
        /// <returns>Returns the service if located, throws an exception if the service is not registered</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not found</exception>
        public TLocatableService GetService<TLocatableService>(ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) where TLocatableService : class
        {
            var result = LocateServiceInternal(typeof(TLocatableService), searchMode);

            if (result is TLocatableService service)
                return service;

            throw new InvalidOperationException($"Service of type {typeof(TLocatableService).Name} not found");
        }
        
        public bool TryGetService(Type serviceType, out object service)
        {
            return TryLocateService(serviceType, out service, ServiceSearchMode.LocalFirst);
        }

        /// <summary>
        /// Tries to locate a service and returns true if the service is found.
        /// </summary>
        /// <param name="service">The service if found or null if not</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <typeparam name="TLocatableService">The type of service to locate</typeparam>
        /// <returns></returns>
        public bool TryLocateService<TLocatableService>(out TLocatableService service, ServiceSearchMode searchMode = ServiceSearchMode.LocalFirst) where TLocatableService : class
        {
            service = LocateServiceInternal(typeof(TLocatableService), searchMode) as TLocatableService;
            return service != null;
        }
        
        
        /// <summary>
        /// Tries to locate a service and returns true if the service is found.
        /// </summary>
        /// <param name="type">The type of service to locate</param>
        /// <param name="service">The service if found or null if not</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <returns></returns>
        public bool TryLocateService(Type type, out object service, ServiceSearchMode searchMode = ServiceSearchMode.LocalFirst)
        {
            service = LocateServiceInternal(type, searchMode);
            return service != null;
        }

        private object LocateServiceInternal(Type type, ServiceSearchMode searchMode)
        {
            return searchMode switch
            {
                ServiceSearchMode.LocalOnly => RegisteredServices.GetValueOrDefault(type),
                ServiceSearchMode.GlobalFirst => _parentContainer != null && _parentContainer.TryLocateService(type, out var service, searchMode) ? service : LocateServiceInternal(type, ServiceSearchMode.LocalOnly),
                ServiceSearchMode.LocalFirst => RegisteredServices.GetValueOrDefault(type) ?? _parentContainer?.LocateServiceInternal(type, searchMode),
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
            _pendingQueries.Clear();

            Injector?.ClearInjectors();
        }
    }
}