using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DGP.ServiceLocator
{
    public static class ServiceInjector
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        
        private struct PendingMethod
        {
            public MethodInfo Method;
            public object Target;
        }
        
        private static List<PendingMethod> _pendingMethods = new List<PendingMethod>(32);
        private static List<Type> _pendingTypes = new List<Type>(16);
        
        public static void Inject(object target) {
            var type = target.GetType();
            
            InjectFields(target, type);
            InjectProperties(target, type);
            InjectMethods(target, type);
        }

        private static void InjectFields(object target, Type type) {
            var fields = type.GetFields(Flags);
            foreach (var field in fields) {
                var attributes = field.GetCustomAttributes(typeof(InjectAttribute), true);
                if (attributes.Length == 0) continue;
                
                var injectAttribute = (InjectAttribute)attributes[0];

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
        }

        private static void InjectProperties(object target, Type type) {
            var properties = type.GetProperties(Flags);
            foreach (var property in properties) {
                var attributes = property.GetCustomAttributes(typeof(InjectAttribute), true);
                if (attributes.Length == 0) continue;
                
                var injectAttribute = (InjectAttribute)attributes[0];

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
        }

        private static void InjectMethods(object target, Type type) {
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
        
        private static bool IsMethodPending(MethodInfo method, object target) {
            foreach (var pendingMethod in _pendingMethods) {
                if (pendingMethod.Method == method && pendingMethod.Target == target)
                    return true;
            }

            return false;
        }

        private static void HandleServiceLocated(object _) {
            for (int i = _pendingMethods.Count - 1; i >= 0; i--) {
                var method = _pendingMethods[i];
                var type = _pendingTypes[i];
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
    }
}