using DGP.ServiceLocator.Extensions;
using UnityEngine;

namespace DGP.ServiceLocator.Injectable
{
    public abstract class InjectedMonoBehaviour : MonoBehaviour
    {
        protected virtual void Awake() => this.InjectServices();
    }
}