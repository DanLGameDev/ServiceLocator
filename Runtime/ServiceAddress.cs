using System;

namespace DGP.ServiceLocator
{
    internal readonly struct ServiceAddress : IEquatable<ServiceAddress>
    {
        public readonly Type Type;
        public readonly object Context;
            
        public ServiceAddress(Type type, object context = null) {
            Type = type;
            Context = context;
        }

        public bool Equals(ServiceAddress other) => Equals(other.Type, other.Context);
        private bool Equals(Type type, object context) => ReferenceEquals(Type, type) && Equals(Context, context);
        public override int GetHashCode() => HashCode.Combine(Type, Context);
    }
}