using UnityEngine;

namespace DGP.ServiceLocator.Injectable
{
    public static class MonoBehaviourExtensions
    {
        public static void InjectServices(this MonoBehaviour target)
        {
            ServiceLocator.Inject(target);
        }
    }
}