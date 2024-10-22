using System.Linq;
using System.Reflection;
using DGP.ServiceLocator.Extensions;

namespace DGP.ServiceLocator.Injectable
{
    public static class ObjectInjectorExtensions
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        
        public static T InjectLocalServices<T>(this object source, T target)
        {
            return InjectFromSource(source, target);
        }
        
        public static T CreateWithLocalServices<T>(this object source) where T : class
        {
            return ConstructWithLocalServices<T>(source);
        }
        
        /// <summary>
        /// Injects any fields or properties marked with the [Provide] attribute from the source object into the target object.
        /// </summary>
        /// <param name="source">The object providing the services</param>
        /// <param name="target">The object needing the services</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T InjectFromSource<T>(object source, T target) {
            var sourceFields = source.GetFieldsWithAttribute<ProvideAttribute>(Flags);
            var sourceProperties = source.GetPropertiesWithAttribute<ProvideAttribute>(Flags);

            InjectPropertiesToTarget(source, target, sourceProperties);
            InjectFieldsToTarget(source, target, sourceFields);

            return target;
        }
        
        private static T ConstructWithLocalServices<T>(object source) {
            var sourceType = source.GetType();
            var sourceFields = source.GetFieldsWithAttribute<ProvideAttribute>(Flags);
            var sourceProperties = source.GetPropertiesWithAttribute<ProvideAttribute>(Flags);

            var constructors = typeof(T).GetConstructors(Flags);
            foreach (var constructor in constructors) {
                var constructorParameters = constructor.GetParameters();
                
                var parameterTypes = constructorParameters.Select(parameter => parameter.ParameterType).ToArray();
                var resolvedTypes = new object[parameterTypes.Length];
                
                for (int i = 0; i < parameterTypes.Length; i++) {
                    var parameterType = parameterTypes[i];
                    object resolvedParameterValue = source.GetType() == parameterType ? source : null;
                    resolvedParameterValue ??= sourceFields.FirstOrDefault(field => field.fieldInfo.FieldType == parameterType).fieldInfo.GetValue(source);
                    resolvedParameterValue ??= sourceProperties.FirstOrDefault(property => property.propertyInfo.PropertyType == parameterType).propertyInfo.GetValue(source);
                    
                    resolvedTypes[i] = resolvedParameterValue;
                }
                
                if (resolvedTypes.Any(resolvedType => resolvedType == null)) continue;
                
                var target = (T) constructor.Invoke(resolvedTypes);
                InjectPropertiesToTarget(source, target, sourceProperties);
                InjectFieldsToTarget(source, target, sourceFields);
                return target;
            }
            
            return default;
        }

        private static void InjectPropertiesToTarget<T>(object source, T target, (PropertyInfo propertyInfo, ProvideAttribute provideAttribute)[] sourceProperties) {
            foreach (var (sourceProperty, provideAttribute) in sourceProperties) {
                var serviceType = provideAttribute.ServiceType ?? sourceProperty.PropertyType;
                var value = sourceProperty.GetValue(source);

                var targetFields = target.GetSettableFieldsWithAttribute<InjectAttribute>(Flags);
                foreach (var (targetField,_) in targetFields) {
                    if (targetField.FieldType != serviceType) continue;
                    targetField.SetValue(target, value);
                }
                
                var targetProperties = target.GetSettablePropertiesWithAttribute<InjectAttribute>(Flags);
                foreach (var (targetProperty,_) in targetProperties) {
                    if (targetProperty.PropertyType != serviceType) continue;
                    targetProperty.SetValue(target, value);
                }
            }
        }

        private static void InjectFieldsToTarget<T>(object source, T target, (FieldInfo fieldInfo, ProvideAttribute provideAttribute)[] sourceFields) {
            foreach (var (sourceField, provideAttribute) in sourceFields) {
                var serviceType = provideAttribute.ServiceType ?? sourceField.FieldType;
                var value = sourceField.GetValue(source);
                
                var targetFields = target.GetSettableFieldsWithAttribute<InjectAttribute>(Flags);
                foreach (var (targetField,_) in targetFields) {
                    if (targetField.FieldType != serviceType) continue;
                    targetField.SetValue(target, value);
                }
                
                var targetProperties = target.GetSettablePropertiesWithAttribute<InjectAttribute>(Flags);
                foreach (var (targetProperty,_) in targetProperties) {
                    if (targetProperty.PropertyType != serviceType) continue;
                    targetProperty.SetValue(target, value);
                }
            }
        }
    }
}