using UnityEngine;

namespace DGP.ServiceLocator.Injectable
{
    public static class MonoBehaviourInjectorExtensions
    {
        public static void InjectServices(this MonoBehaviour target, ServiceContainer container = null) {
            if (container != null)
                container.Injector.Inject(target);
            else
                ServiceLocator.Injector.Inject(target);
        }
    }
}