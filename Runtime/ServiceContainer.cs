using System;
using System.Collections.Generic;
using DGP.ServiceLocator.Injectable;

namespace DGP.ServiceLocator
{
    public class ServiceContainer
    {
        public readonly Dictionary<ServiceAddress, ILocatableService> RegisteredServices = new();
        public readonly List<ServiceQuery> PendingServiceQueries = new();

        private readonly Lazy<ServiceInjector> _injector;
        public ServiceInjector Injector => _injector.Value;
        
        public ServiceContainer() {
            _injector = new Lazy<ServiceInjector>(() => new ServiceInjector(this));
        }
        
        #region Registration
        /// <summary>
        /// Registers a service with the ServiceLocator
        /// </summary>
        /// <param name="service">The service to register, must implement ILocatableService</param>
        /// <param name="context">An optional context for the service</param>
        /// <typeparam name="TLocatableService">The type of service, must implement ILocatableService</typeparam>
        public void RegisterService<TLocatableService>(TLocatableService service, object context=null) where TLocatableService : class, ILocatableService {
            RegisterService(typeof(TLocatableService), service, context);
        }

        private void RegisterService(Type type, ILocatableService service, object context=null) {
            ServiceAddress address = FindOrCreateServiceAddress(type, context);
            RegisteredServices[address] = service;
            
            FlushPendingServiceQueries();
        }

        /// <summary>
        /// Deregisters a service from the ServiceLocator
        /// </summary>
        /// <param name="context">The context the service uses</param>
        /// <typeparam name="TLocatableService">The type of service to deregister</typeparam>
        public void DeregisterService<TLocatableService>(object context=null) where TLocatableService : class, ILocatableService {
            DeregisterService(typeof(TLocatableService), context);
        }

        private void DeregisterService(Type type, object context=null) {
            ServiceAddress address = FindOrCreateServiceAddress(type, context);
            RegisteredServices.Remove(address);
        }
        
        /// <summary>
        /// Deregisters all services associated with a context
        /// </summary>
        /// <param name="context"></param>
        public void DeregisterContext(object context) {
            foreach (var address in RegisteredServices.Keys) {
                if (Equals(address.Context, context)) {
                    RegisteredServices.Remove(address);
                }
            }
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
        public void LocateServiceAsync<TLocatableService>(Action<TLocatableService> callback, object context=null, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService {
            LocateServiceAsync(typeof(TLocatableService), service => callback((TLocatableService)service), context, searchMode);
        }
        
        public void LocateServiceAsync(Type type, Action<ILocatableService> callback, object context=null, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) {
            var result = LocateServiceInternal(type, context, searchMode);

            if (result != null) {
                callback((ILocatableService)result);
                return;
            }
            
            PendingServiceQueries.Add(new ServiceQuery {
                SearchMode = searchMode,
                Address = new ServiceAddress(type, context),
                CallbackFn = callback
            });
        }
        
        private void FlushPendingServiceQueries() {
            for (int index = PendingServiceQueries.Count - 1; index >= 0; index--) {
                var query = PendingServiceQueries[index];
                
                var result = LocateServiceInternal(query.Address.Type, query.Address.Context, query.SearchMode);

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
        public TLocatableService GetService<TLocatableService>(object context=null, ServiceSearchMode searchMode=ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService {
            var result = LocateServiceInternal(typeof(TLocatableService), context, searchMode);

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
        public bool TryLocateService<TLocatableService>(out TLocatableService service, object context=null, ServiceSearchMode searchMode=ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService {
            service = LocateServiceInternal(typeof(TLocatableService), context, searchMode) as TLocatableService;
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
        public bool TryLocateService(Type type, out ILocatableService service, object context=null, ServiceSearchMode searchMode=ServiceSearchMode.GlobalFirst) {
            service = LocateServiceInternal(type, context, searchMode) as ILocatableService;
            return service != null;
        }

        private object LocateServiceInternal(Type type, object context, ServiceSearchMode searchMode) {
            Func<Type, object, object> locateService = (serviceType, context) => {
                foreach (var entry in RegisteredServices) {
                    var address = entry.Key;
                    
                    if (address.Type == serviceType && Equals(address.Context, context)) {
                        return entry.Value;
                    }
                }
                
                return null;
            };
            
            switch (searchMode) {
                case ServiceSearchMode.GlobalOnly:
                    return locateService(type, null);
                case ServiceSearchMode.LocalOnly:
                    return locateService(type, context);
                case ServiceSearchMode.GlobalFirst: {
                    object service = locateService(type, null);
                    return service ?? locateService(type, context);
                }
                case ServiceSearchMode.LocalFirst: {
                    object service = locateService(type, context);
                    return service ?? locateService(type, null);
                }
                default:
                    return null;
            }
        }
        
        #endregion
        
        private ServiceAddress FindOrCreateServiceAddress(Type type, object context) {
            foreach (var address in RegisteredServices.Keys) {
                if ((address.Context == context) && (address.Type == type)) {
                    return address;
                }
            }
            
            return new ServiceAddress(type, context);
        }
        
        /// <summary>
        /// Clears all services and pending service queries
        /// </summary>
        public void ClearServices() {
            RegisteredServices.Clear();
            PendingServiceQueries.Clear();
            
            if (Injector != null)
                Injector.ClearInjectors();
        }
    }
}