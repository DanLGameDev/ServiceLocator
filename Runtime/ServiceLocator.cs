using System;
using DGP.ServiceLocator.Injectable;
using UnityEditor;

namespace DGP.ServiceLocator
{
    public static class ServiceLocator
    {
        private static readonly Lazy<ServiceContainer> instance = new(() => new ServiceContainer());
        public static ServiceContainer Instance => instance.Value;
        public static ServiceContainer Container => Instance;
        public static ServiceInjector Injector => Instance.Injector;

#if UNITY_EDITOR
        static ServiceLocator()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChange;
            EditorApplication.playModeStateChanged += PlayModeStateChange;
        }
        
        private static void PlayModeStateChange(PlayModeStateChange obj)
        {
            if (obj == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                ClearServices();
            }
        }
#endif
        
        #region Registration

        /// <summary>
        /// Registers a service with the ServiceLocator
        /// </summary>
        /// <param name="service">The service to register, must implement ILocatableService</param>
        /// <typeparam name="TLocatableService">The type of service, must implement ILocatableService</typeparam>
        public static TLocatableService RegisterService<TLocatableService>(TLocatableService service)
            where TLocatableService : class
        {
            Instance.RegisterService(service);
            return service;
        }

        public static object RegisterService(Type type, object service)
        {
            Instance.RegisterService(type, service);
            return service;
        }

        /// <summary>
        /// Deregisters a service from the ServiceLocator
        /// </summary>
        /// <typeparam name="TLocatableService">The type of service to deregister</typeparam>
        public static void DeregisterService<TLocatableService>() where TLocatableService : class
        {
            Instance.DeregisterService<TLocatableService>();
        }

        /// <summary>
        /// De-registers a service from the ServiceLocator
        /// </summary>
        /// <param name="type">The type to deregister</param>
        /// <exception cref="ArgumentNullException">thrown if type is null</exception>
        /// <exception cref="ArgumentException">thrown if type does not implement ILocatableService</exception>
        public static void DeregisterService(Type type)
        {
            Instance.DeregisterService(type);
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
        public static void LocateServiceAsync<TLocatableService>(Action<TLocatableService> callback, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst)
            where TLocatableService : class
        {
            Instance.LocateServiceAsync(callback, searchMode);
        }

        /// <summary>
        /// Locates a service and invokes a callback. If the service is not found, the callback will be
        /// invoked when the service is registered.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        /// <param name="searchMode"></param>
        public static void LocateServiceAsync(Type type, Action<object> callback, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst)
        {
            Instance.LocateServiceAsync(type, callback, searchMode);
        }

        /// <summary>
        /// Locates a service synchronously and returns it. If the service is not found, null is returned.
        /// </summary>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <typeparam name="TLocatableService">The type of service to locate</typeparam>
        /// <returns>Returns the service if located, throws an exception if the service is not registered</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not found</exception>
        public static TLocatableService GetService<TLocatableService>(
            ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst)
            where TLocatableService : class
        {
            return Instance.GetService<TLocatableService>(searchMode);
        }

        /// <summary>
        /// Tries to locate a service and returns true if the service is found.
        /// </summary>
        /// <param name="service">The service if found or null if not</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <typeparam name="TLocatableService">The type of service to locate</typeparam>
        /// <returns></returns>
        public static bool TryLocateService<TLocatableService>(out TLocatableService service,
            ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst)
            where TLocatableService : class
        {
            return Instance.TryLocateService(out service, searchMode);
        }

        /// <summary>
        /// Tries to locate a service and returns true if the service is found.
        /// </summary>
        /// <param name="type">The type of service to locate</param>
        /// <param name="service">The service if found or null if not</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <returns></returns>
        public static bool TryLocateService(Type type, out object service,
            ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst)
        {
            return Instance.TryLocateService(type, out service, searchMode);
        }

        #endregion

        /// <summary>
        /// Clears all services and pending service queries
        /// </summary>
        public static void ClearServices()
        {
            Instance.ClearServices();
        }
    }
}