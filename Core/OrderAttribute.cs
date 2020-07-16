using System;

namespace Cloudy
{
    /// <summary>
    /// Allows ordering components for processing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    class OrderAttribute : Attribute
    {
        public OrderAttribute(int order) => Order = order;

        public int Order { get; }
    }
}
