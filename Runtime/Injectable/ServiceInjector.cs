using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DGP.ServiceLocator.Injectable
{
    public class ServiceInjector
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        
        private struct PendingMethod
        {
            public MethodInfo Method;
            public object Target;
        }
     
        private readonly ServiceContainer _serviceContainer;
        private readonly List<PendingMethod> _pendingMethods = new List<PendingMethod>(32);
        private readonly List<Type> _pendingTypes = new List<Type>(16);
        
        public ServiceInjector(ServiceContainer serviceContainer) {
            _serviceContainer = serviceContainer;
        }
        
        public void Inject(object target) {
            var type = target.GetType();
            
            InjectFields(target, type);
            InjectProperties(target, type);
            InjectMethods(target, type);
        }

        private void InjectFields(object target, Type type) {
            var fields = type.GetFields(Flags);
            foreach (var field in fields) {
                InjectField(target, field);
            }
        }

        private void InjectField(object target, FieldInfo field) {
            if (field.IsInitOnly)
                throw new System.Exception($"Cannot inject into readonly field {field.Name}");
                
            var attributes = field.GetCustomAttributes(typeof(InjectAttribute), true);
            if (attributes.Length == 0) return;
                
            var injectAttribute = (InjectAttribute)attributes[0];
                
            if (injectAttribute.Flags.HasFlag(InjectorFlags.DontReplace) && field.GetValue(target) != null)
                return;
                
            if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous)) {
                ServiceLocator.LocateServiceAsync(field.FieldType, service => {
                    field.SetValue(target, service);
                });
            } else if (ServiceLocator.TryLocateService(field.FieldType, out ILocatableService service)) {
                field.SetValue(target, service);
            } else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                throw new System.Exception($"Missing dependency for {field.Name}");
            }
        }

        private void InjectProperties(object target, Type type) {
            var properties = type.GetProperties(Flags);
            foreach (var property in properties) {
                InjectProperty(target, property);
            }
        }

        private void InjectProperty(object target, PropertyInfo property) {
            if (property.CanWrite == false || property.GetSetMethod(true) == null)
                throw new System.Exception($"Cannot inject into readonly property {property.Name}");
                
            var attributes = property.GetCustomAttributes(typeof(InjectAttribute), true);
            if (attributes.Length == 0) return;
                
            var injectAttribute = (InjectAttribute)attributes[0];
                
            if (injectAttribute.Flags.HasFlag(InjectorFlags.DontReplace) && property.GetValue(target) != null)
                return;

            if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous)) {
                ServiceLocator.LocateServiceAsync(property.PropertyType, service => {
                    property.SetValue(target, service);
                });
            } else if (ServiceLocator.TryLocateService(property.PropertyType, out ILocatableService service)) {
                property.SetValue(target, service);
            } else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                throw new System.Exception($"Missing dependency for {property.Name}");
            }
        }

        private void InjectMethods(object target, Type type) {
            var methods = type.GetMethods(Flags);
            foreach (var method in methods) {
                if (IsMethodPending(method, target)) continue;
                
                var attributes = method.GetCustomAttributes(typeof(InjectAttribute), true);
                if (attributes.Length == 0) continue;
                
                var injectAttribute = (InjectAttribute)attributes[0];
                
                var requiredParams = method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
                
                object[] resolvedInstances = requiredParams
                    .Select(paramType => ServiceLocator.TryLocateService(paramType, out var service) ? service : null)
                    .ToArray<object>();

                if (resolvedInstances.All(instance => instance != null)) {
                    method.Invoke(target, resolvedInstances);
                } else if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous)) {
                    for (int i = 0; i < requiredParams.Length; i++) {
                        if (resolvedInstances[i] == null && !_pendingTypes.Contains(requiredParams[i])) {
                            ServiceLocator.LocateServiceAsync(requiredParams[i], HandleServiceLocated);
                            _pendingTypes.Add(requiredParams[i]);
                        }
                    }
                    
                    if (!IsMethodPending(method, target))
                        _pendingMethods.Add(new PendingMethod { Method = method, Target = target });
                } else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                    throw new System.Exception($"Missing dependency for {method.Name}");
                }
            }
        }

        public T CreateAndInject<T>() where T : class {
            var constructors = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (constructors.Length == 0)
                throw new System.Exception($"No constructors found for {typeof(T).Name}");
            
            //find a constructor we can satisfy
            foreach (var constructor in constructors) {
                // see if the constructor is marked as Inject
                var attributes = constructor.GetCustomAttributes(typeof(InjectAttribute), true);
                if (attributes.Length == 0) continue;
                
                var injectAttribute = (InjectAttribute)attributes[0];
                
                if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous))
                    throw new System.Exception("Cannot use asynchronous injection on constructors");
                
                var parameters = constructor.GetParameters();
                object[] resolvedInstances = parameters
                    .Select(parameter => ServiceLocator.TryLocateService(parameter.ParameterType, out var service) ? service : null)
                    .ToArray();
                
                if (injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                    return (T)constructor.Invoke(resolvedInstances);
                } else if (resolvedInstances.All(instance => instance != null)) {
                    return (T)constructor.Invoke(resolvedInstances);
                }
            }

            return null;
        }
        
        private bool IsMethodPending(MethodInfo method, object target) {
            foreach (var pendingMethod in _pendingMethods) {
                if (pendingMethod.Method == method && pendingMethod.Target == target)
                    return true;
            }

            return false;
        }

        private void HandleServiceLocated(object _) {
            for (int i = _pendingMethods.Count - 1; i >= 0; i--) {
                var method = _pendingMethods[i];
                
                var requiredParams = method.Method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
                
                object[] resolvedInstances = requiredParams
                    .Select(paramType => ServiceLocator.TryLocateService(paramType, out var service) ? service : null)
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

        public void ClearInjectors() {
            _pendingMethods.Clear();
            _pendingTypes.Clear();
        }


        public static T InjectFromSource<T>(object source, T target) {
            var sourceFields = source.GetType().GetFields(Flags);

            foreach (var sourceField in sourceFields) {
                var provideAttributes = sourceField.GetCustomAttributes(typeof(ProvideAttribute), true);
                if (provideAttributes.Length == 0) continue;
                
                var provideAttribute = (ProvideAttribute)provideAttributes[0];
                var serviceType = provideAttribute.ServiceType ?? sourceField.FieldType;
                
                var value = sourceField.GetValue(source);
                
                var targetFields = target.GetType().GetFields(Flags);
                foreach (var targetField in targetFields) {
                    if (targetField.FieldType != serviceType) continue;
                    
                    var injectAttributes = targetField.GetCustomAttributes(typeof(InjectAttribute), true);
                    if (injectAttributes.Length == 0) continue;
                    
                    targetField.SetValue(target, value);
                }
                
                var targetProperties = target.GetType().GetProperties(Flags);
                foreach (var targetProperty in targetProperties) {
                    if (targetProperty.PropertyType != serviceType) continue;
                    
                    var injectAttributes = targetProperty.GetCustomAttributes(typeof(InjectAttribute), true);
                    if (injectAttributes.Length == 0) continue;
                    
                    targetProperty.SetValue(target, value);
                }
            }
            
            var sourceProperties = source.GetType().GetProperties(Flags);
            
            foreach (var sourceProperty in sourceProperties) {
                var provideAttributes = sourceProperty.GetCustomAttributes(typeof(ProvideAttribute), true);
                if (provideAttributes.Length == 0) continue;
                
                var provideAttribute = (ProvideAttribute)provideAttributes[0];
                var serviceType = provideAttribute.ServiceType ?? sourceProperty.PropertyType;
                
                var value = sourceProperty.GetValue(source);
                
                var targetFields = target.GetType().GetFields(Flags);
                foreach (var targetField in targetFields) {
                    if (targetField.FieldType != serviceType) continue;
                    
                    var injectAttributes = targetField.GetCustomAttributes(typeof(InjectAttribute), true);
                    if (injectAttributes.Length == 0) continue;
                    
                    targetField.SetValue(target, value);
                }
                
                var targetProperties = target.GetType().GetProperties(Flags);
                foreach (var targetProperty in targetProperties) {
                    if (targetProperty.PropertyType != serviceType) continue;
                    
                    var injectAttributes = targetProperty.GetCustomAttributes(typeof(InjectAttribute), true);
                    if (injectAttributes.Length == 0) continue;
                    
                    targetProperty.SetValue(target, value);
                }
            }

            return target;
        }
    }
}