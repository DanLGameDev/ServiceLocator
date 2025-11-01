using System;

namespace DGP.ServiceLocator.Injectable
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ProvideAttribute : Attribute
    {
        public readonly Type ServiceType;
        
        public ProvideAttribute(Type serviceType = null)
        {
            ServiceType = serviceType;
        }
    }
}