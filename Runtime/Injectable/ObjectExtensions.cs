
using System.Reflection;

namespace DGP.ServiceLocator.Injectable
{
    public static class ObjectExtensions
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        
        public static T InjectLocalServices<T>(this object source, T target)
        {
            return ObjectExtensions.InjectFromSource(source, target);
        }
        
        /// <summary>
        /// Injects any fields or properties marked with the [Provide] attribute from the source object into the target object.
        /// </summary>
        /// <param name="source">The object providing the services</param>
        /// <param name="target">The object needing the services</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T InjectFromSource<T>(object source, T target) {
            var sourceFields = GetProvidedFields(source);
            var sourceProperties = GetProvidedProperties(source);

            InjectFieldsToTarget(source, target, sourceFields);
            InjectPropertiesToTarget(source, target, sourceProperties);

            return target;
        }

        private static void InjectPropertiesToTarget<T>(object source, T target, PropertyInfo[] sourceProperties) {
            foreach (var sourceProperty in sourceProperties) {
                var provideAttributes = sourceProperty.GetCustomAttributes(typeof(ProvideAttribute), true);
                
                if (provideAttributes.Length == 0) continue;
                
                var serviceType = ((ProvideAttribute)provideAttributes[0]).ServiceType ?? sourceProperty.PropertyType;
                
                var value = sourceProperty.GetValue(source);
                
                var targetFields = target.GetType().GetFields(Flags);
                foreach (var targetField in targetFields) {
                    if (targetField.FieldType != serviceType) continue;
                    
                    if (FieldIsInjectable(targetField))
                        targetField.SetValue(target, value);
                }
                
                var targetProperties = target.GetType().GetProperties(Flags);
                foreach (var targetProperty in targetProperties) {
                    if (targetProperty.PropertyType != serviceType) continue;
                    
                    if (PropertyIsInjectable(targetProperty))
                        targetProperty.SetValue(target, value);
                }
            }
        }

        private static void InjectFieldsToTarget<T>(object source, T target, FieldInfo[] sourceFields) {
            foreach (var sourceField in sourceFields) {
                var provideAttributes = sourceField.GetCustomAttributes(typeof(ProvideAttribute), true);
                if (provideAttributes.Length == 0) continue;
                
                var serviceType = ((ProvideAttribute)provideAttributes[0]).ServiceType ?? sourceField.FieldType;
                
                var value = sourceField.GetValue(source);
                
                var targetFields = target.GetType().GetFields(Flags);
                foreach (var targetField in targetFields) {
                    if (targetField.FieldType != serviceType) continue;
                    
                    if (FieldIsInjectable(targetField))
                        targetField.SetValue(target, value);
                }
                
                var targetProperties = target.GetType().GetProperties(Flags);
                foreach (var targetProperty in targetProperties) {
                    if (targetProperty.PropertyType != serviceType) continue;
                    
                    if (PropertyIsInjectable(targetProperty))
                        targetProperty.SetValue(target, value);
                }
            }
        }

        static bool FieldIsInjectable(FieldInfo field) {
            var attributes = field.GetCustomAttributes(typeof(InjectAttribute), true);
            return attributes.Length > 0;
        }
        
        static bool PropertyIsInjectable(PropertyInfo property) {
            var attributes = property.GetCustomAttributes(typeof(InjectAttribute), true);
            return attributes.Length > 0;
        }
        
        static MethodInfo[] GetProvidedMethods(object source) {
            var methods = source.GetType().GetMethods(Flags);
            var returnedMethods = new MethodInfo[methods.Length];
            
            for (var i = 0; i < methods.Length; i++) {
                var method = methods[i];
                var attributes = method.GetCustomAttributes(typeof(ProvideAttribute), true);
                if (attributes.Length == 0) continue;
                
                returnedMethods[i] = method;
            }
            
            return returnedMethods;
        }
        
        static PropertyInfo[] GetProvidedProperties(object source) {
            var props = source.GetType().GetProperties(Flags);
            var returnedProps = new PropertyInfo[props.Length];
            
            for (var i = 0; i < props.Length; i++) {
                var prop = props[i];
                var attributes = prop.GetCustomAttributes(typeof(ProvideAttribute), true);
                if (attributes.Length == 0) continue;
                
                returnedProps[i] = prop;
            }
            
            return returnedProps;
        }
        
        static FieldInfo[] GetProvidedFields(object source) {
            var fields = source.GetType().GetFields(Flags);
            var returnedFields = new FieldInfo[fields.Length];
            
            for (var i = 0; i < fields.Length; i++) {
                var field = fields[i];
                var attributes = field.GetCustomAttributes(typeof(ProvideAttribute), true);
                if (attributes.Length == 0) continue;
                
                returnedFields[i] = field;
            }
            
            return returnedFields;
        }
    }
}