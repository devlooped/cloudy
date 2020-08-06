#pragma warning disable CS0618 // Type or member is obsolete (while in preview ¯\_(ツ)_/¯)
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudy
{
    /// <summary>
    /// Exception filter that detects exceptions of type <see cref="HttpStatusCodeException"/> and 
    /// sets the HTTP response status accordingly.
    /// </summary>
    [ServiceLifetime(ServiceLifetime.Singleton)]
    public class HttpStatusCodeExceptionFilter : IFunctionExceptionFilter
    {
        readonly IHttpContextAccessor httpAccessor;

        public HttpStatusCodeExceptionFilter(IHttpContextAccessor httpAccessor) => this.httpAccessor = httpAccessor;

        public async Task OnExceptionAsync(FunctionExceptionContext context, CancellationToken cancellation)
        {
            if (context.Exception.InnerException is HttpStatusCodeException ex)
            {
                httpAccessor.HttpContext.Response.StatusCode = (int)ex.StatusCode;
                await httpAccessor.HttpContext.Response.WriteAsync(ex.StatusCode.ToString());
            }
        }
    }
}
