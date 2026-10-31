using System;
using System.Collections.Generic;

namespace DGP.ServiceLocator.Injectable
{
    public class ServiceInjector
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceInjector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Fills every [Inject] field and property on the target, then calls its [Inject] methods.
        /// Nothing is written unless every required dependency can be supplied.
        /// </summary>
        /// <exception cref="MissingDependencyException">A required dependency's service was not supplied</exception>
        /// <exception cref="InvalidOperationException">The target's type marks an [Inject] point that can't be injected</exception>
        public void Inject(object target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!TryInject(target, InjectionPlan.For(target.GetType()), out var missing))
                throw missing;
        }

        /// <summary>
        /// Constructs T through its [Inject] constructor, or else the constructor with the most parameters that can be satisfied,
        /// then injects it as <see cref="Inject"/> does.
        /// </summary>
        /// <exception cref="MissingDependencyException">No constructor or required dependency could be satisfied</exception>
        /// <exception cref="InvalidOperationException">T marks an [Inject] point that can't be injected</exception>
        public T Create<T>() where T : class
        {
            if (!TryCreate<T>(out var instance, out var missing))
                throw missing;

            return instance;
        }

        /// <summary>
        /// As <see cref="Create{T}"/>, but returns false instead of throwing when a dependency is missing.
        /// </summary>
        /// <exception cref="InvalidOperationException">T marks an [Inject] point that can't be injected</exception>
        public bool TryCreate<T>(out T instance) where T : class => TryCreate(out instance, out _);

        private bool TryCreate<T>(out T instance, out MissingDependencyException missing) where T : class
        {
            instance = null;

            var plan = InjectionPlan.For(typeof(T));

            if (!TryConstruct(plan, out var constructed, out missing))
                return false;

            if (!TryInject(constructed, plan, out missing))
                return false;

            instance = (T)constructed;
            return true;
        }

        private bool TryConstruct(InjectionPlan plan, out object instance, out MissingDependencyException missing)
        {
            instance = null;
            missing = null;

            foreach (var constructor in plan.ConstructorPoints) {
                if (TryResolveServices(constructor.ServiceTypes, out var services, out var missingType)) {
                    instance = constructor.Constructor.Invoke(services);
                    missing = null;
                    return true;
                }

                missing ??= new MissingDependencyException(plan.TargetType, "constructor", missingType);
            }

            return false;
        }

        private bool TryInject(object target, InjectionPlan plan, out MissingDependencyException missing)
        {
            var resolved = new List<(InjectionPoint point, object[] services)>(plan.Points.Length);

            foreach (var point in plan.Points) {
                if (point.IsAlreadySet(target))
                    continue;

                if (TryResolveServices(point.ServiceTypes, out var services, out var missingType)) {
                    resolved.Add((point, services));
                    continue;
                }

                if (point.IsOptional)
                    continue;

                missing = new MissingDependencyException(plan.TargetType, point.MemberName, missingType);
                return false;
            }

            foreach (var (point, services) in resolved) {
                point.Apply(target, services);
            }

            missing = null;
            return true;
        }

        private bool TryResolveServices(Type[] serviceTypes, out object[] services, out Type missingType)
        {
            services = new object[serviceTypes.Length];

            for (var i = 0; i < serviceTypes.Length; i++) {
                if (!_serviceProvider.TryGetService(serviceTypes[i], out services[i]) || services[i] == null) {
                    missingType = serviceTypes[i];
                    return false;
                }
            }

            missingType = null;
            return true;
        }
    }
}
