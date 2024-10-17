using System;

namespace DGP.ServiceLocator.Injectable
{
    [Flags]
    public enum InjectorFlags
    {
        None = 0,
        
        // Will not throw an exception if the dependency is missing
        Optional = 1 << 1,
        
        // If the dependency is not available, the injection will be performed when the dependency is available
        Asynchronous = 1 << 2,
        
        // Will not replace the existing value if it is already set
        DontReplace = 1 << 3,
    }
        
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor)]
    public class InjectAttribute : Attribute
    {
        public readonly InjectorFlags Flags;
        
        public InjectAttribute(InjectorFlags flags=default(InjectorFlags))
        {
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