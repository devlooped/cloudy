using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Cloudy
{
    // Usability overloads for Activity APIs
    public static class ActivityExtensions
    {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        public static Activity2? StartActivity(this ActivitySource source, [CallerMemberName] string name = "", ActivityKind kind = ActivityKind.Internal, params KeyValuePair<string, string>[] tags)
            => source.StartActivity(name, kind, tags);

#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

        // See https://github.com/dotnet/runtime/issues/39866
        public static bool TryGetBaggage(this Activity activity, string key, out string? value)
        {
            value = activity.Baggage.LastOrDefault(pair => pair.Key == key).Value;
            return value != default;
        }

        // See https://github.com/dotnet/runtime/issues/39866
        public static bool TryGetTag(this Activity activity, string key, out string? value)
        {
            value = activity.Tags.LastOrDefault(pair => pair.Key == key).Value;
            return value != default;
        }
    }
}
