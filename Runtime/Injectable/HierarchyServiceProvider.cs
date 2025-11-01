using DGP.ServiceLocator.Extensions;
using UnityEngine;

namespace DGP.ServiceLocator.Injectable
{
    /// <summary>
    /// Internal IServiceProvider implementation that searches the GameObject hierarchy,
    /// then falls back to the global ServiceLocator.
    /// </summary>
    internal class HierarchyServiceProvider : IServiceProvider
    {
        private readonly GameObject _gameObject;

        public HierarchyServiceProvider(GameObject gameObject)
        {
            _gameObject = gameObject;
        }

        public bool TryGetService(System.Type serviceType, out object service)
        {
            // First: Try hierarchy search
            if (_gameObject.TryGetServiceFromHierarchy(serviceType, out service))
                return true;

            // Fallback: Try global ServiceLocator
            return ServiceLocator.Container.TryLocateService(serviceType, out service);
        }
    }
}