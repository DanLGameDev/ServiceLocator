using System;
using System.Collections.Generic;
using DGP.ServiceLocator.Injectable;
using UnityEditor;
using UnityEngine;

namespace DGP.ServiceLocator
{
    public static class ServiceLocator
    {
        internal static readonly ServiceContainer Instance;
        public static ServiceContainer Container => Instance;
        public static ServiceInjector Injector => Instance.Injector;

        static ServiceLocator() {
            Instance = new ServiceContainer();
            
            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= PlayModeStateChange;
            EditorApplication.playModeStateChanged += PlayModeStateChange;
            #endif
        }
        
        private static void PlayModeStateChange(PlayModeStateChange obj) {
            if (obj == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                ClearServices();
            }
        }

        #region Registration
        /// <summary>
        /// Registers a service with the ServiceLocator
        /// </summary>
        /// <param name="service">The service to register, must implement ILocatableService</param>
        /// <typeparam name="TLocatableService">The type of service, must implement ILocatableService</typeparam>
        public static void RegisterService<TLocatableService>(TLocatableService service) where TLocatableService : class, ILocatableService {
            Instance.RegisterService(service);
        }
        
        public static void RegisterService(Type type, ILocatableService service) {
            Instance.RegisterService(type, service);
        }
        
        /// <summary>
        /// Deregisters a service from the ServiceLocator
        /// </summary>
        /// <typeparam name="TLocatableService">The type of service to deregister</typeparam>
        public static void DeregisterService<TLocatableService>() where TLocatableService : class, ILocatableService {
            Instance.DeregisterService<TLocatableService>();
        }
        
        /// <summary>
        /// De-registers a service from the ServiceLocator
        /// </summary>
        /// <param name="type">The type to deregister</param>
        /// <exception cref="ArgumentNullException">thrown if type is null</exception>
        /// <exception cref="ArgumentException">thrown if type does not implement ILocatableService</exception>
        public static void DeregisterService(Type type) {
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
        public static void LocateServiceAsync<TLocatableService>(Action<TLocatableService> callback, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService {
            Instance.LocateServiceAsync(callback, searchMode);
        }
        
        public static void LocateServiceAsync(System.Type type, Action<ILocatableService> callback, ServiceSearchMode searchMode = ServiceSearchMode.GlobalFirst) {
            Instance.LocateServiceAsync(type, callback, searchMode);
        }
        
        /// <summary>
        /// Locates a service synchronously and returns it. If the service is not found, null is returned.
        /// </summary>
        /// <param name="context">An optional context the service must exist in</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <typeparam name="TLocatableService">The type of service to locate</typeparam>
        /// <returns>Returns the service if located, throws an exception if the service is not registered</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not found</exception>
        public static TLocatableService GetService<TLocatableService>(ServiceSearchMode searchMode=ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService {
            return Instance.GetService<TLocatableService>(searchMode);
        }
        
        /// <summary>
        /// Tries to locate a service and returns true if the service is found.
        /// </summary>
        /// <param name="service">The service if found or null if not</param>
        /// <param name="context">The context the service must exist in</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <typeparam name="TLocatableService">The type of service to locate</typeparam>
        /// <returns></returns>
        public static bool TryLocateService<TLocatableService>(out TLocatableService service, ServiceSearchMode searchMode=ServiceSearchMode.GlobalFirst) where TLocatableService : class, ILocatableService {
            return Instance.TryLocateService(out service, searchMode);
        }
        
        /// <summary>
        /// Tries to locate a service and returns true if the service is found.
        /// </summary>
        /// <param name="type">The type of service to locate</param>
        /// <param name="service">The service if found or null if not</param>
        /// <param name="context">The context the service must exist in</param>
        /// <param name="searchMode">The search mode to use when locating the service</param>
        /// <returns></returns>
        public static bool TryLocateService(Type type, out ILocatableService service, ServiceSearchMode searchMode=ServiceSearchMode.GlobalFirst) {
            return Instance.TryLocateService(type, out service, searchMode);
        }
        #endregion
        
        /// <summary>
        /// Clears all services and pending service queries
        /// </summary>
        public static void ClearServices() {
            Instance.ClearServices();
        }
    }
}