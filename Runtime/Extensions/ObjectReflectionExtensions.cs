using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DGP.ServiceLocator.Extensions
{
    public static class ObjectReflectionExtensions
    {
        public static (PropertyInfo propertyInfo, TAttributeType attribute)[] GetSettablePropertiesWithAttribute<TAttributeType>(this object target, BindingFlags flags) where TAttributeType : Attribute {
            var properties = GetPropertiesWithAttribute<TAttributeType>(target, flags);
            return properties.Where(property => property.propertyInfo.CanWrite && property.propertyInfo.SetMethod != null).ToArray();
        }
        
        public static (FieldInfo fieldInfo, TAttributeType attribute)[] GetSettableFieldsWithAttribute<TAttributeType>(this object target, BindingFlags flags) where TAttributeType : Attribute {
            var fields = GetFieldsWithAttribute<TAttributeType>(target, flags);
            return fields.Where(field => !field.fieldInfo.IsInitOnly).ToArray();
        }
        
        public static (PropertyInfo propertyInfo, TAttributeType attribute)[] GetPropertiesWithAttribute<TAttributeType>(this object target, BindingFlags flags) where TAttributeType : Attribute {
            return GetMembersOfTypeWithAttribute<PropertyInfo, TAttributeType>(target.GetType().GetProperties(flags));
        }
        
        public static (FieldInfo fieldInfo, TAttributeType attribute)[] GetFieldsWithAttribute<TAttributeType>(this object target, BindingFlags flags) where TAttributeType : Attribute {
            return GetMembersOfTypeWithAttribute<FieldInfo, TAttributeType>(target.GetType().GetFields(flags));
        }
        
        public static (MethodInfo methodInfo, TAttributeType attribute)[] GetMethodsWithAttribute<TAttributeType>(this object target, BindingFlags flags) where TAttributeType : Attribute {
            return GetMembersOfTypeWithAttribute<MethodInfo, TAttributeType>(target.GetType().GetMethods(flags));
        }
        
        public static (ConstructorInfo constructorInfo, TAttributeType attribute)[] GetConstructorsWithAttribute<TAttributeType>(this object target, BindingFlags flags) where TAttributeType : Attribute {
            return GetMembersOfTypeWithAttribute<ConstructorInfo, TAttributeType>(target.GetType().GetConstructors(flags));
        }
        
        private static (TMemberType memberInfo, TAttributeType attribute)[] GetMembersOfTypeWithAttribute<TMemberType, TAttributeType>(TMemberType[] members) where TMemberType : MemberInfo where TAttributeType : Attribute {
            List<(TMemberType, TAttributeType)> membersWithAttribute = new(members.Length);

            foreach (TMemberType member in members) {
                var attribute = member.GetCustomAttribute<TAttributeType>();
                
                if (attribute != null)
                    membersWithAttribute.Add((member, attribute));
            }
            
            return membersWithAttribute.ToArray();
        }
    }
}