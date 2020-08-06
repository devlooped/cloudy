#pragma warning disable CS0618 // Type or member is obsolete (while in preview ¯\_(ツ)_/¯)
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudy
{

    /// <summary>
    /// Implements the <see cref="IFunctionInvocationFilter"/> that in turn invokes the 
    /// registered <see cref="IHttpFunctionInvocationFilter"/> for HTTP-triggered functions.
    /// </summary>
    [ServiceLifetime(ServiceLifetime.Singleton)]
    public class HttpFunctionInvocationFilter : IFunctionInvocationFilter
    {
        ConcurrentDictionary<string, (Type? Type, MethodInfo? Method)> methods = new ConcurrentDictionary<string, (Type?, MethodInfo?)>();
        readonly IHttpContextAccessor accessor;
        readonly IEnumerable<IHttpFunctionInvocationFilter> filters;

        public HttpFunctionInvocationFilter(IHttpContextAccessor accessor, IEnumerable<IHttpFunctionInvocationFilter> filters) 
            => (this.accessor, this.filters)
            = (accessor, filters);

        public async Task OnExecutingAsync(FunctionExecutingContext functionContext, CancellationToken cancellation) 
            => await InvokeFiltersAsync(async (filter, httpContext, functionDescriptor, customAttributes)
                => await filter.OnExecutingAsync(httpContext, functionContext, functionDescriptor, customAttributes, cancellation));

        public async Task OnExecutedAsync(FunctionExecutedContext functionContext, CancellationToken cancellation)
            => await InvokeFiltersAsync(async (filter, httpContext, functionDescriptor, customAttributes)
                => await filter.OnExecutedAsync(httpContext, functionContext, functionDescriptor, customAttributes, cancellation));

        async Task InvokeFiltersAsync(Func<IHttpFunctionInvocationFilter, HttpContext, dynamic, ICustomAttributeProvider, Task> callback)
        {
            var http = accessor.HttpContext;
            var feature = http.Features.Where(kv => kv.Key.Name == "IFunctionExecutionFeature")
                .Select(kv => kv.Value.AsDynamicReflection())
                .FirstOrDefault();

            if (feature == null)
                return;

            dynamic descriptor = feature.Descriptor;

            // This only applies to HTTP-triggered functions.
            if ("httpTrigger".Equals((string)descriptor.Metadata.Trigger.Type, StringComparison.OrdinalIgnoreCase))
            {
                var function = methods.GetOrAdd((string)descriptor.Metadata.EntryPoint, FindFunctionMethod);
                if (function.Method != null)
                {
                    var attributes = new AggregateAttributesProvider(function.Method!, function.Type!);
                    foreach (var filter in filters)
                    {
                        await callback(filter, http, descriptor, attributes);
                    }
                }
            }
        }

        static (Type? Type, MethodInfo? Method) FindFunctionMethod(string entryPoint)
        {
            var typeName = string.Join('.', entryPoint.Split('.')[..^1]);
            var methodName = entryPoint.Split('.')[^1];

            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(asm => asm.GetType(typeName))
                .FirstOrDefault(type => type != null);
            var method = type?.GetMethod(methodName);

            return (type, method);
        }

        class AggregateAttributesProvider : ICustomAttributeProvider
        {
            readonly ICustomAttributeProvider[] attributeProviders;

            public AggregateAttributesProvider(params ICustomAttributeProvider[] attributeProviders)
                => this.attributeProviders = attributeProviders;

            public object[] GetCustomAttributes(bool inherit) 
                => attributeProviders.SelectMany(x => x.GetCustomAttributes(inherit)).ToArray();

            public object[] GetCustomAttributes(Type attributeType, bool inherit)
                => attributeProviders.SelectMany(x => x.GetCustomAttributes(attributeType, inherit)).ToArray();

            public bool IsDefined(Type attributeType, bool inherit)
                => attributeProviders.Any(x => x.IsDefined(attributeType, inherit));
        }
    }
}
