using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.Utilities;

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
            var fields = GetInjectableFields(target, type);
            foreach (var field in fields) {
                InjectField(target, field);
            }
        }
        
        private void InjectField(object target, FieldInfo field) {
            var injectAttribute = FindInjectAttribute(field);
            
            if (injectAttribute == null)
                throw new Exception($"Cannot find attribute on field marked for injection {field.Name}");
            
            Type intendedType = injectAttribute.ServiceType ?? field.FieldType;
            
            if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous)) {
                ServiceLocator.LocateServiceAsync(intendedType, service => {
                    field.SetValue(target, service);
                });
            } else if (ServiceLocator.TryLocateService(intendedType, out ILocatableService service)) {
                field.SetValue(target, service);
            } else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                throw new Exception($"Missing dependency for {field.Name}");
            }
        }
        
        private void InjectProperties(object target, Type type) {
            var properties = GetInjectableProperties(target, type);
            foreach (var property in properties) {
                InjectProperty(target, property);
            }
        }

        private void InjectProperty(object target, PropertyInfo property) {
            if (property.CanWrite == false || property.GetSetMethod(true) == null)
                throw new System.Exception($"Cannot inject into readonly property {property.Name}");
                
            var injectAttribute = FindInjectAttribute(property);
            
            if (injectAttribute == null)
                throw new Exception($"Cannot find attribute on field marked for injection {property.Name}");
            
            Type intendedType = injectAttribute.ServiceType ?? property.PropertyType;
            
            if (injectAttribute.Flags.HasFlag(InjectorFlags.Asynchronous)) {
                ServiceLocator.LocateServiceAsync(intendedType, service => {
                    property.SetValue(target, service);
                });
            } else if (ServiceLocator.TryLocateService(intendedType, out ILocatableService service)) {
                property.SetValue(target, service);
            } else if (!injectAttribute.Flags.HasFlag(InjectorFlags.Optional)) {
                throw new System.Exception($"Missing dependency for {property.Name}");
            }
        }

        private void InjectMethods(object target, Type type) {
            var methods = type.GetMethods(Flags);
            foreach (var method in methods) {
                if (IsMethodPending(method, target))
                    continue;
                
                var injectAttribute = FindInjectAttribute(method);
                
                if (injectAttribute == null)
                    continue;
                
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
        
        #region Member Location
        private InjectAttribute FindInjectAttribute(MemberInfo member) {
            var attributes = member.GetCustomAttributes(typeof(InjectAttribute), true);
            if (attributes.Length == 0) return null;
            
            return (InjectAttribute)attributes[0];
        }
        
        private FieldInfo[] GetInjectableFields(object target, Type type) {
            var fields = type.GetFields(Flags);
            
            foreach (var field in fields) {
                if (field.IsInitOnly)
                    throw new Exception($"Cannot inject into readonly field {field.Name}");
            }
            
            return GetInjectableMembersOfType(target, fields);
        }
        
        private PropertyInfo[] GetInjectableProperties(object target, Type type) {
            var properties = type.GetProperties(Flags);

            foreach (var property in properties) {
                if (property.CanWrite == false || property.GetSetMethod(true) == null)
                    throw new Exception($"Cannot inject into readonly property {property.Name}");
            }

            return GetInjectableMembersOfType(target, properties);
        }
        
        private MethodInfo[] GetInjectableMethods(object target, Type type) {
            var methods = type.GetMethods(Flags);
            return GetInjectableMembersOfType(target, methods);
        }

        private T[] GetInjectableMembersOfType<T>(object target, T[] members) where T : MemberInfo {
            List<T> injectableMembers = new List<T>(members.Length);

            foreach (T member in members) {
                var injectAttribute = FindInjectAttribute(member);
                if (injectAttribute == null) continue;
                
                if (injectAttribute.Flags.HasFlag(InjectorFlags.DontReplace) && member.GetMemberValue(target) != null)
                    continue;
                
                injectableMembers.Add(member);
            }
            
            return injectableMembers.ToArray();
        }
        #endregion



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


    }
}