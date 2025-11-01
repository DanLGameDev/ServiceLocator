using System;
using System.Reflection;
using DGP.ServiceLocator.Extensions;

namespace DGP.ServiceLocator.Injectable
{
    public static class ServiceProviderExtensions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        /// Tries to get a service from an object's [Provide] fields or properties
        /// </summary>
        /// <param name="source">The object to search for provided services</param>
        /// <param name="serviceType">The type of service to locate</param>
        /// <param name="service">The service instance if found, null otherwise</param>
        /// <returns>True if a matching service was found, false otherwise</returns>
        public static bool TryGetProvidedService(this object source, Type serviceType, out object service)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            // Check fields with [Provide] attribute
            var fields = source.GetFieldsWithAttribute<Injectable.ProvideAttribute>(Flags);
            foreach (var (fieldInfo, provideAttribute) in fields)
            {
                var providedType = provideAttribute.ServiceType ?? fieldInfo.FieldType;
                if (providedType == serviceType)
                {
                    service = fieldInfo.GetValue(source);
                    return service != null;
                }
            }

            // Check properties with [Provide] attribute
            var properties = source.GetPropertiesWithAttribute<Injectable.ProvideAttribute>(Flags);
            foreach (var (propertyInfo, provideAttribute) in properties)
            {
                var providedType = provideAttribute.ServiceType ?? propertyInfo.PropertyType;
                if (providedType == serviceType)
                {
                    service = propertyInfo.GetValue(source);
                    return service != null;
                }
            }

            service = null;
            return false;
        }
    }
}