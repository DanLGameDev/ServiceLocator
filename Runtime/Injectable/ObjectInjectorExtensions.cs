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