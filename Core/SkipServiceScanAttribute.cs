using System;

namespace Cloudy
{
    /// <summary>
    /// Prevents a type from being automatically registed in the 
    /// DI container. Useful for cases where the dependency is 
    /// constructed and/or registered separately.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    class SkipServiceScanAttribute : Attribute
    {
    }
}
