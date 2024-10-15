using System;

namespace DGP.ServiceLocator
{
    [Flags]
    public enum InjectorFlags
    {
        // Will not throw an exception if the dependency is missing
        None = 0,
        
        // Will throw an exception if the dependency is missing
        ExceptionIfMissing = 1 << 1,
        
        // If the dependency is not available, the injection will be performed when the dependency is available
        Asynchronous = 1 << 2,
    }
        
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class InjectAttribute : Attribute
    {
        public readonly InjectorFlags Flags;
        
        public InjectAttribute(InjectorFlags flags=InjectorFlags.None)
        {
            Flags = flags;
        }
    }
}