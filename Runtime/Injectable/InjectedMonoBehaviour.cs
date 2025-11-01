using UnityEngine;

namespace DGP.ServiceLocator.Injectable
{
    public abstract class InjectedMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Controls whether to search up the GameObject hierarchy for services before falling back to the global ServiceLocator.
        /// Default is true.
        /// </summary>
        protected virtual bool SearchHierarchy => true;

        protected virtual void Awake()
        {
            if (SearchHierarchy)
                this.InjectFromHierarchy();
            else
                this.InjectFromGlobal();
        }
    }
}