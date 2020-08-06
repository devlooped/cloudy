#pragma warning disable CS0618 // Type or member is obsolete (while in preview ¯\_(ツ)_/¯)
using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudy
{
    /// <summary>
    /// Example function that uses a filter that causes the response status to be 
    /// set to a given code.
    /// </summary>
    public static class StatusCodeFunction
    {
        [HarcodedStatusFilter(HttpStatusCode.Unauthorized)]
        [FunctionName("forbidden")]
        public static Task<IActionResult> ForbiddenAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
            => Task.FromResult<IActionResult>(new OkResult());
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HarcodedStatusFilterAttribute : Attribute
    {
        public HarcodedStatusFilterAttribute(HttpStatusCode statusCode) => StatusCode = statusCode;

        public HttpStatusCode StatusCode { get; }
    }

    [ServiceLifetime(ServiceLifetime.Singleton)]
    public class HarcodedStatusFilter : IHttpFunctionInvocationFilter
    {
        public Task OnExecutedAsync(HttpContext httpContext, FunctionExecutedContext functionContext, dynamic functionDescriptor, ICustomAttributeProvider customAttributes, CancellationToken cancellation)
            => Task.CompletedTask;

        public Task OnExecutingAsync(HttpContext httpContext, FunctionExecutingContext functionContext, dynamic functionDescriptor, ICustomAttributeProvider customAttributes, CancellationToken cancellation)
        {
            var attr = customAttributes.GetCustomAttribute<HarcodedStatusFilterAttribute>();
            if (attr != null)
                throw new HttpStatusCodeException(attr.StatusCode);

            return Task.CompletedTask;
        }
    }
}
