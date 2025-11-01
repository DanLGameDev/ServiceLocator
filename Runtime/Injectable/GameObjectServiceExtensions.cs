using System;
using DGP.ServiceLocator.Injectable;
using UnityEngine;

namespace DGP.ServiceLocator.Extensions
{
    public static class GameObjectServiceExtensions
    {
        /// <summary>
        /// Searches up the GameObject hierarchy for a service of the specified type.
        /// First checks for IServiceProvider components, then falls back to checking for [Provide] attributes.
        /// </summary>
        /// <param name="go">The GameObject to start searching from</param>
        /// <param name="serviceType">The type of service to locate</param>
        /// <param name="service">The service instance if found, null otherwise</param>
        /// <returns>True if a matching service was found in the hierarchy, false otherwise</returns>
        public static bool TryGetServiceFromHierarchy(this GameObject go, Type serviceType, out object service)
        {
            if (go == null)
                throw new ArgumentNullException(nameof(go));
            
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            Transform current = go.transform;
            
            while (current != null)
            {
                // First: Check for explicit IServiceProvider components
                var provider = current.GetComponent<IServiceProvider>();
                if (provider != null && provider.TryGetService(serviceType, out service))
                    return true;
                
                // Fallback: Check all components for [Provide] attributes
                var components = current.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    if (component != null && component.TryGetProvidedService(serviceType, out service))
                        return true;
                }
                
                current = current.parent;
            }
            
            service = null;
            return false;
        }

        /// <summary>
        /// Searches up the GameObject hierarchy for a service of the specified type.
        /// First checks for IServiceProvider components, then falls back to checking for [Provide] attributes.
        /// </summary>
        /// <typeparam name="T">The type of service to locate</typeparam>
        /// <param name="go">The GameObject to start searching from</param>
        /// <param name="service">The service instance if found, default otherwise</param>
        /// <returns>True if a matching service was found in the hierarchy, false otherwise</returns>
        public static bool TryGetServiceFromHierarchy<T>(this GameObject go, out T service)
        {
            if (TryGetServiceFromHierarchy(go, typeof(T), out object serviceObj))
            {
                service = (T)serviceObj;
                return true;
            }
            
            service = default;
            return false;
        }

        /// <summary>
        /// Gets a service from the GameObject hierarchy or throws an exception if not found.
        /// </summary>
        /// <typeparam name="T">The type of service to locate</typeparam>
        /// <param name="go">The GameObject to start searching from</param>
        /// <returns>The service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not found in the hierarchy</exception>
        public static T GetServiceFromHierarchy<T>(this GameObject go)
        {
            if (TryGetServiceFromHierarchy<T>(go, out var service))
                return service;
            
            throw new InvalidOperationException($"Service of type {typeof(T).Name} not found in GameObject hierarchy starting from {go.name}");
        }
    }
}