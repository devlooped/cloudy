using System;

namespace Cloudy
{
    /// <summary>
    /// Registers the service only if the <c>ASPNETCORE_ENVIRONMENT</c> environment 
    /// variable matches the given value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    class ServiceEnvironmentAttribute : Attribute
    {
        public ServiceEnvironmentAttribute(string environment) => Environment = environment;

        public string Environment { get; }
    }
}
