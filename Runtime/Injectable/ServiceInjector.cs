using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DGP.ServiceLocator.Extensions;

namespace DGP.ServiceLocator.Injectable
{
    public class ServiceInjector
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        private struct PendingMethod
        {
            public MethodInfo Method;
            public object Target;
        }

        private readonly ServiceContainer _serviceContainer;
        private readonly List<PendingMethod> _pendingMethods = new List<PendingMethod>(32);
        private readonly List<Type> _pendingTypes = new List<Type>(16);

        public ServiceInjector(ServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
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

            foreach (var (fieldInfo, injectAttribute) in fields) {
                if (injectAttribute.Flags.HasFlag(InjectorFlags.DontReplace) && fieldInfo.GetMemberValueOrNull(target) != null)
                    continue;

                InjectField(target, fieldInfo, injectAttribute);
            }
        }

        private void InjectField(object target, FieldInfo field, InjectAttribute injectAttribute)
        {
            Type intendedType = injectAttribute.ServiceType ?? field.FieldType;

            if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous)) {
                _serviceContainer.LocateServiceAsync(intendedType, service => { field.SetValue(target, service); });
            }
            else if (_serviceContainer.TryLocateService(intendedType, out ILocatableService service)) {
                field.SetValue(target, service);
            }
            else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                throw new Exception($"Missing dependency for {field.Name}");
            }
        }

        private void InjectProperties(object target)
        {
            var properties = target.GetSettablePropertiesWithAttribute<InjectAttribute>(Flags);

            foreach (var (propertyInfo, injectAttribute) in properties) {
                if (injectAttribute.Flags.HasFlag(InjectorFlags.DontReplace) && propertyInfo.GetMemberValueOrNull(target) != null)
                    continue;

                InjectProperty(target, propertyInfo, injectAttribute);
            }
        }

        private void InjectProperty(object target, PropertyInfo property, InjectAttribute injectAttribute)
        {
            Type intendedType = injectAttribute.ServiceType ?? property.PropertyType;

            if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous)) {
                _serviceContainer.LocateServiceAsync(intendedType, service => { property.SetValue(target, service); });
            }
            else if (_serviceContainer.TryLocateService(intendedType, out ILocatableService service)) {
                property.SetValue(target, service);
            }
            else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                throw new System.Exception($"Missing dependency for {property.Name}");
            }
        }

        private void InjectMethods(object target)
        {
            var methodPair = target.GetMethodsWithAttribute<InjectAttribute>(Flags);

            foreach (var (method, injectAttribute) in methodPair) {
                if (IsMethodAlreadyPending(method, target))
                    continue;

                var requiredParams = method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();

                object[] resolvedInstances = requiredParams
                    .Select(paramType => _serviceContainer.TryLocateService(paramType, out var service) ? service : null)
                    .ToArray<object>();

                if (resolvedInstances.All(instance => instance != null)) {
                    method.Invoke(target, resolvedInstances);
                }
                else if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous)) {
                    for (int i = 0; i < requiredParams.Length; i++) {
                        if (resolvedInstances[i] == null && !_pendingTypes.Contains(requiredParams[i])) {
                            _serviceContainer.LocateServiceAsync(requiredParams[i], HandleServiceLocated);
                            _pendingTypes.Add(requiredParams[i]);
                        }
                    }

                    if (!IsMethodAlreadyPending(method, target))
                        _pendingMethods.Add(new PendingMethod { Method = method, Target = target });
                }
                else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                    throw new System.Exception($"Missing dependency for {method.Name}");
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
                throw new System.Exception("Cannot find any valid constructors for type " + typeof(T).Name);

            foreach (var constructor in constructors) {
                var parameters = constructor.GetParameters();
                object[] resolvedInstances = parameters
                    .Select(parameter => _serviceContainer.TryLocateService(parameter.ParameterType, out var service) ? service : null)
                    .ToArray();

                if (resolvedInstances.All(instance => instance != null)) {
                    return (T)constructor.Invoke(resolvedInstances);
                }
            }

            return null;
        }

        private bool IsMethodAlreadyPending(MethodInfo method, object target)
        {
            foreach (var pendingMethod in _pendingMethods) {
                if (pendingMethod.Method == method && pendingMethod.Target == target)
                    return true;
            }

            return false;
        }

        private void HandleServiceLocated(object _)
        {
            for (int i = _pendingMethods.Count - 1; i >= 0; i--) {
                var method = _pendingMethods[i];

                var requiredParams = method.Method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();

                object[] resolvedInstances = requiredParams
                    .Select(paramType => _serviceContainer.TryLocateService(paramType, out var service) ? service : null)
                    .ToArray();

                if (resolvedInstances.All(instance => instance != null)) {
                    method.Method.Invoke(method.Target, resolvedInstances);
                    _pendingMethods.RemoveAt(i);
                    i--;

                    foreach (var requiredParam in requiredParams) {
                        _pendingTypes.Remove(requiredParam);
                    }

                    continue;
                }

                // Could not fulfill the dependency so we will clean up the pending list and try again later
                foreach (var resolvedInstance in resolvedInstances) {
                    if (resolvedInstance != null)
                        _pendingTypes.Remove(resolvedInstance.GetType());
                }
            }
        }

        public void ClearInjectors()
        {
            _pendingMethods.Clear();
            _pendingTypes.Clear();
        }
    }
}