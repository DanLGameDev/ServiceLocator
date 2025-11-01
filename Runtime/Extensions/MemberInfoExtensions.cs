using System;
using System.Reflection;

namespace DGP.ServiceLocator.Extensions
{
    public static class MemberInfoExtensions
    {
        public static object GetMemberValueOrNull(this MemberInfo member, object target)
        {
            return member switch
            {
                FieldInfo field => field.GetValue(target),
                PropertyInfo property => property.GetValue(target),
                _ => null
            };
        }

        public static T GetAttributeOrNull<T>(this MemberInfo member) where T : Attribute
        {
            return member.GetCustomAttribute<T>();
        }
    }
}