using System;
using System.Linq;
using System.Reflection;
using DGP.ServiceLocator.Extensions;

namespace DGP.ServiceLocator.Injectable
{
    public class ServiceInjector
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        private readonly IServiceProvider _serviceProvider;

        public ServiceInjector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Inject(object target)
        {
            InjectFields(target);
            InjectProperties(target);
            InjectMethods(target);
        }

        private void InjectFields(object target)
        {
            var fields = target.GetSettableFieldsWithAttribute<InjectAttribute>(Flags);

            foreach (var (fieldInfo, injectAttribute) in fields)
            {
                if (injectAttribute.Flags.HasFlag(InjectorFlags.DontReplace) && fieldInfo.GetMemberValueOrNull(target) != null)
                    continue;

                InjectField(target, fieldInfo, injectAttribute);
            }
        }

        private void InjectField(object target, FieldInfo field, InjectAttribute injectAttribute)
        {
            Type intendedType = injectAttribute.ServiceType ?? field.FieldType;

            if (_serviceProvider.TryGetService(intendedType, out object service))
            {
                field.SetValue(target, service);
            }
            else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional))
            {
                throw new Exception($"Missing dependency for {field.Name}");
            }
        }

        private void InjectProperties(object target)
        {
            var properties = target.GetSettablePropertiesWithAttribute<InjectAttribute>(Flags);

            foreach (var (propertyInfo, injectAttribute) in properties)
            {
                if (injectAttribute.Flags.HasFlag(InjectorFlags.DontReplace) && propertyInfo.GetMemberValueOrNull(target) != null)
                    continue;

                InjectProperty(target, propertyInfo, injectAttribute);
            }
        }

        private void InjectProperty(object target, PropertyInfo property, InjectAttribute injectAttribute)
        {
            Type intendedType = injectAttribute.ServiceType ?? property.PropertyType;

            if (_serviceProvider.TryGetService(intendedType, out object service))
            {
                property.SetValue(target, service);
            }
            else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional))
            {
                throw new Exception($"Missing dependency for {property.Name}");
            }
        }

        private void InjectMethods(object target)
        {
            var methodPair = target.GetMethodsWithAttribute<InjectAttribute>(Flags);

            foreach (var (method, injectAttribute) in methodPair)
            {
                var requiredParams = method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();

                object[] resolvedInstances = requiredParams
                    .Select(paramType => _serviceProvider.TryGetService(paramType, out var service) ? service : null)
                    .ToArray();

                if (resolvedInstances.All(instance => instance != null))
                {
                    method.Invoke(target, resolvedInstances);
                }
                else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional))
                {
                    throw new Exception($"Missing one or more dependencies for {method.Name}");
                }
            }
        }

        public bool TryCreateAndInject<T>(out T instance, bool allowUnmarkedConstructors = true) where T : class
        {
            instance = CreateAndInject<T>(allowUnmarkedConstructors);
            return instance != null;
        }

        public T CreateAndInject<T>(bool allowUnmarkedConstructors = true) where T : class
        {
            var constructorFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            ConstructorInfo[] constructors = null;

            var markedConstructors = typeof(T).GetConstructorsWithAttribute<InjectAttribute>(constructorFlags);

            if (markedConstructors.Length > 0)
                constructors = markedConstructors.Select(constructor => constructor.constructorInfo).ToArray();
            else if (allowUnmarkedConstructors)
                constructors = typeof(T).GetConstructors(constructorFlags);

            if (constructors.Length == 0)
                throw new Exception("Cannot find any valid constructors for type " + typeof(T).Name);

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                object[] resolvedInstances = parameters
                    .Select(parameter => _serviceProvider.TryGetService(parameter.ParameterType, out var service) ? service : null)
                    .ToArray();

                if (resolvedInstances.All(instance => instance != null))
                {
                    return (T)constructor.Invoke(resolvedInstances);
                }
            }

            return null;
        }

        public void ClearInjectors()
        {
            // Reserved for future use if additional state is added
        }
    }
}