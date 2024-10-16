using UnityEngine;

namespace DGP.ServiceLocator
{
    public static class MonoBehaviourExtensions
    {
        public static void InjectServices(this MonoBehaviour target)
        {
            ServiceInjector.Inject(target);
        }
    }
}