using UnityEngine;

namespace DGP.ServiceLocator.Injectable
{
    public static class MonoBehaviourInjectorExtensions
    {
        /// <summary>
        /// Injects services from the GameObject hierarchy, falling back to the global ServiceLocator.
        /// </summary>
        /// <param name="target">The MonoBehaviour to inject services into</param>
        public static void InjectFromHierarchy(this MonoBehaviour target)
        {
            var hierarchyProvider = new HierarchyServiceProvider(target.gameObject);
            var injector = new ServiceInjector(hierarchyProvider);
            injector.Inject(target);
        }

        /// <summary>
        /// Injects services from the global ServiceLocator only (skips hierarchy).
        /// </summary>
        /// <param name="target">The MonoBehaviour to inject services into</param>
        public static void InjectFromGlobal(this MonoBehaviour target)
        {
            ServiceLocator.Injector.Inject(target);
        }

        /// <summary>
        /// Injects services from a specific container.
        /// </summary>
        /// <param name="target">The MonoBehaviour to inject services into</param>
        /// <param name="container">The container to inject from</param>
        public static void InjectFromContainer(this MonoBehaviour target, ServiceContainer container)
        {
            if (container != null)
                container.Injector.Inject(target);
            else
                ServiceLocator.Injector.Inject(target);
        }
    }
}