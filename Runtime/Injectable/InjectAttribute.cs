using System;

namespace DGP.ServiceLocator.Injectable
{
    [Flags]
    public enum InjectorFlags
    {
        None = 0,
        
        // Will not throw an exception if the dependency is missing
        Optional = 1 << 1,
        
        // Will not replace the existing value if it is already set
        DontReplace = 1 << 3,
    }
        
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor)]
    public class InjectAttribute : Attribute
    {
        public readonly Type ServiceType;
        public readonly InjectorFlags Flags;
        
        public InjectAttribute(Type serviceType = null, InjectorFlags flags = default(InjectorFlags))
        {
            ServiceType = serviceType;
            Flags = flags;
        }
    }
    
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