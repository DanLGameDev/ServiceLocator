using UnityEngine;

namespace DGP.ServiceLocator
{
    public abstract class InjectedMonoBehaviour : MonoBehaviour
    {
        protected virtual void Awake() => this.InjectServices();
    }
}