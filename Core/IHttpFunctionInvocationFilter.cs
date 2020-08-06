#pragma warning disable CS0618 // Type or member is obsolete (while in preview ¯\_(ツ)_/¯)
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;

namespace Cloudy
{
    /// <summary>
    /// Interface implemented by filters that run for HTTP-triggered functions 
    /// only.
    /// </summary>
    public interface IHttpFunctionInvocationFilter
    {
        /// <summary>
        /// Invoked when execution of the function execution begins.
        /// </summary>
        /// <param name="httpContext">The current HTTP execution context.</param>
        /// <param name="functionContext">The function execution context.</param>
        /// <param name="functionDescriptor">The <c>FunctionDescriptor</c> (see <see cref="https://github.com/Azure/azure-functions-host/blob/dev/src/WebJobs.Script/Description/FunctionDescriptor.cs"/>).</param>
        /// <param name="customAttributes">The attributes defined in both the method and the type of the running function.</param>
        /// <param name="cancellation">The cancellation token.</param>
        Task OnExecutingAsync(HttpContext httpContext, FunctionExecutingContext functionContext, dynamic functionDescriptor, ICustomAttributeProvider customAttributes, CancellationToken cancellation);

        /// <summary>
        /// Invoked when execution of the function execution finished.
        /// </summary>
        /// <param name="httpContext">The current HTTP execution context.</param>
        /// <param name="functionContext">The function execution context.</param>
        /// <param name="functionDescriptor">The <c>FunctionDescriptor</c> (see <see cref="https://github.com/Azure/azure-functions-host/blob/dev/src/WebJobs.Script/Description/FunctionDescriptor.cs"/>).</param>
        /// <param name="customAttributes">The attributes defined in both the method and the type of the running function.</param>
        /// <param name="cancellation">The cancellation token.</param>
        Task OnExecutedAsync(HttpContext httpContext, FunctionExecutedContext functionContext, dynamic functionDescriptor, ICustomAttributeProvider customAttributes, CancellationToken cancellation);
    }
}
