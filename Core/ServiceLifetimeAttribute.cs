using Microsoft.Extensions.DependencyInjection;
using System;

namespace Cloudy
{
    /// <summary>
    /// Optionally determines the lifetime of an automatically 
    /// registered service. If not provided, default lifetime 
    /// is <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    class ServiceLifetimeAttribute : Attribute
    {
        public ServiceLifetimeAttribute(ServiceLifetime lifetime) => Lifetime = lifetime;

        public ServiceLifetime Lifetime { get; }
    }
}
