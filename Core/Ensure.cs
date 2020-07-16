using System;
using System.Diagnostics;

namespace Cloudy
{
    public static class Ensure
    {
        [DebuggerStepThrough]
        public static string NotEmpty(this string? value, string? name)
        {
            if (value == null)
                throw new ArgumentNullException(name ?? nameof(value));

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be empty.", name ?? nameof(value));

            return value;
        }
    }
}
