using System.Linq;

namespace System.Reflection
{
    public static class ICustomAttributeProviderExtensions
    {
        public static T? GetCustomAttribute<T>(this ICustomAttributeProvider element, bool inherit) where T : Attribute
            => element.GetCustomAttributes(inherit).OfType<T>().FirstOrDefault();

        public static T? GetCustomAttribute<T>(this ICustomAttributeProvider element) where T : Attribute
            => element.GetCustomAttributes(false).OfType<T>().FirstOrDefault();
    }
}
