using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DGP.ServiceLocator.Injectable
{
    internal class InjectionPoint
    {
        public string MemberName;
        public Type[] ServiceTypes;
        public InjectorFlags Flags;
        public Func<object, object> ReadValue;
        public Action<object, object[]> Apply;

        public bool IsOptional => Flags.HasFlag(InjectorFlags.Optional);

        public bool IsAlreadySet(object target) => Flags.HasFlag(InjectorFlags.DontReplace) && ReadValue(target) != null;
    }

    internal class ConstructorPoint
    {
        public ConstructorInfo Constructor;
        public Type[] ServiceTypes;
    }

    internal class InjectionPlan
    {
        private const BindingFlags DeclaredMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        private const BindingFlags Constructors = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly ConcurrentDictionary<Type, InjectionPlan> Plans = new();
        private static readonly Assembly UnityCoreAssembly = typeof(MonoBehaviour).Assembly;

        public readonly Type TargetType;
        public readonly InjectionPoint[] Points;
        public readonly ConstructorPoint[] ConstructorPoints;

        private InjectionPlan(Type targetType)
        {
            TargetType = targetType;

            var hierarchy = GetInjectableHierarchy(targetType);

            Points = BuildFieldPoints(hierarchy)
                .Concat(BuildPropertyPoints(hierarchy))
                .Concat(BuildMethodPoints(hierarchy))
                .ToArray();

            ConstructorPoints = BuildConstructorPoints(targetType);
        }

        public static InjectionPlan For(Type targetType) => Plans.GetOrAdd(targetType, type => new InjectionPlan(type));

        private static Type[] GetInjectableHierarchy(Type type)
        {
            var hierarchy = new List<Type>();

            // Private members of base classes are only visible when each level is reflected on its own.
            // Unity's base types never carry [Inject], so the walk stops there.
            for (var current = type; current != null && current != typeof(object) && current.Assembly != UnityCoreAssembly; current = current.BaseType) {
                hierarchy.Add(current);
            }

            hierarchy.Reverse();
            return hierarchy.ToArray();
        }

        private static IEnumerable<InjectionPoint> BuildFieldPoints(Type[] hierarchy) =>
            FindMarked(hierarchy.SelectMany(type => type.GetFields(DeclaredMembers)))
                .Select(marked => CreateFieldPoint(marked.member, marked.attribute));

        private static IEnumerable<InjectionPoint> BuildPropertyPoints(Type[] hierarchy) =>
            FindMarked(hierarchy.SelectMany(type => type.GetProperties(DeclaredMembers)))
                .GroupBy(marked => (MemberInfo)marked.member.SetMethod?.GetBaseDefinition() ?? marked.member)
                .Select(overrides => CreatePropertyPoint(overrides.First().member, overrides.First().attribute));

        // An overridden [Inject] method is invoked once through its base declaration; virtual dispatch reaches the override.
        private static IEnumerable<InjectionPoint> BuildMethodPoints(Type[] hierarchy) =>
            FindMarked(hierarchy.SelectMany(type => type.GetMethods(DeclaredMembers)))
                .GroupBy(marked => marked.member.GetBaseDefinition())
                .Select(overrides => CreateMethodPoint(overrides.First().member, overrides.First().attribute));

        private static IEnumerable<(TMember member, InjectAttribute attribute)> FindMarked<TMember>(IEnumerable<TMember> members) where TMember : MemberInfo =>
            members
                .Select(member => (member, attribute: member.GetCustomAttribute<InjectAttribute>(false)))
                .Where(marked => marked.attribute != null);

        private static InjectionPoint CreateFieldPoint(FieldInfo field, InjectAttribute attribute)
        {
            if (field.IsInitOnly)
                throw InvalidPoint(field, "is readonly");

            return new InjectionPoint
            {
                MemberName = field.Name,
                ServiceTypes = new[] { GetServiceType(field, field.FieldType, attribute) },
                Flags = attribute.Flags,
                ReadValue = field.GetValue,
                Apply = (target, services) => field.SetValue(target, services[0])
            };
        }

        private static InjectionPoint CreatePropertyPoint(PropertyInfo property, InjectAttribute attribute)
        {
            if (property.SetMethod == null)
                throw InvalidPoint(property, "has no setter");

            if (attribute.Flags.HasFlag(InjectorFlags.DontReplace) && property.GetMethod == null)
                throw InvalidPoint(property, "uses DontReplace but has no getter");

            return new InjectionPoint
            {
                MemberName = property.Name,
                ServiceTypes = new[] { GetServiceType(property, property.PropertyType, attribute) },
                Flags = attribute.Flags,
                ReadValue = property.GetValue,
                Apply = (target, services) => property.SetValue(target, services[0])
            };
        }

        private static InjectionPoint CreateMethodPoint(MethodInfo method, InjectAttribute attribute)
        {
            if (attribute.ServiceType != null)
                throw InvalidPoint(method, "sets a ServiceType, which only fields and properties support");

            if (attribute.Flags.HasFlag(InjectorFlags.DontReplace))
                throw InvalidPoint(method, "uses DontReplace, which only fields and properties support");

            return new InjectionPoint
            {
                MemberName = method.Name,
                ServiceTypes = GetParameterTypes(method),
                Flags = attribute.Flags,
                Apply = (target, services) => method.Invoke(target, services)
            };
        }

        private static ConstructorPoint[] BuildConstructorPoints(Type type)
        {
            var constructors = type.GetConstructors(Constructors);
            var marked = FindMarked(constructors).ToArray();

            if (marked.Length > 1)
                throw new InvalidOperationException($"{type.Name} marks {marked.Length} constructors with [Inject]; mark at most one");

            if (marked.Length == 1) {
                var (constructor, attribute) = marked[0];

                if (attribute.ServiceType != null || attribute.Flags != InjectorFlags.None)
                    throw InvalidPoint(constructor, "sets a ServiceType or flags, which constructors don't support");

                return new[] { CreateConstructorPoint(constructor) };
            }

            return constructors
                .OrderByDescending(constructor => constructor.GetParameters().Length)
                .Select(CreateConstructorPoint)
                .ToArray();
        }

        private static ConstructorPoint CreateConstructorPoint(ConstructorInfo constructor) =>
            new() { Constructor = constructor, ServiceTypes = GetParameterTypes(constructor) };

        private static Type[] GetParameterTypes(MethodBase method) =>
            method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        private static Type GetServiceType(MemberInfo member, Type memberType, InjectAttribute attribute)
        {
            var serviceType = attribute.ServiceType ?? memberType;

            if (!memberType.IsAssignableFrom(serviceType))
                throw InvalidPoint(member, $"asks for {serviceType.Name}, which can't be assigned to {memberType.Name}");

            return serviceType;
        }

        private static InvalidOperationException InvalidPoint(MemberInfo member, string problem) =>
            new($"[Inject] on {member.DeclaringType?.Name}.{member.Name} is invalid: it {problem}");
    }
}
