using System;

namespace DGP.ServiceLocator.Injectable
{
    public class MissingDependencyException : Exception
    {
        public readonly Type TargetType;
        public readonly string MemberName;
        public readonly Type ServiceType;

        public MissingDependencyException(Type targetType, string memberName, Type serviceType)
            : base($"{targetType.Name}.{memberName} requires {serviceType.Name}, which no provider supplied")
        {
            TargetType = targetType;
            MemberName = memberName;
            ServiceType = serviceType;
        }
    }
}
