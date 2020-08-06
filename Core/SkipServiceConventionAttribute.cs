using System;

namespace Cloudy
{
    /// <summary>
    /// Prevents a type from being automatically registed in the 
    /// DI container by the <see cref="ServiceConvention"/>. 
    /// Useful for cases where the dependency is constructed and/or registered manually.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    class SkipServiceConventionAttribute : Attribute
    {
    }
}
