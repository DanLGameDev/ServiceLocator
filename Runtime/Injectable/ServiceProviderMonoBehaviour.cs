using System;
using UnityEngine;

namespace DGP.ServiceLocator.Injectable
{
    /// <summary>
    /// A MonoBehaviour component that provides services via reflection of [Provide] attributes.
    /// Attach this component to a GameObject to make it discoverable as a service provider in the hierarchy.
    /// </summary>
    public class ServiceProviderMonoBehaviour : MonoBehaviour, IServiceProvider
    {
        public bool TryGetService(Type serviceType, out object service)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            // Search all components on this GameObject for [Provide] attributes
            var components = GetComponents<MonoBehaviour>();
            
            foreach (var component in components)
            {
                if (component != null && component.TryGetProvidedService(serviceType, out service))
                    return true;
            }
            
            service = null;
            return false;
        }
    }
}