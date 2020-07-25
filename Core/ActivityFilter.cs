#pragma warning disable CS0618 // For some reason, all Filter APIs are marked obsolete because of potential breaking changes
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudy
{
    [ServiceLifetime(ServiceLifetime.Singleton)]
    class ActivityFilter : IFunctionInvocationFilter
    {
        readonly ActivitySource source;
        readonly TelemetryClient telemetry;
        readonly ConcurrentDictionary<Guid, Activity?> activities = new ConcurrentDictionary<Guid, Activity?>();

        public ActivityFilter(ActivitySource source, TelemetryClient telemetry) 
            => (this.source, this.telemetry)
            = (source, telemetry);

        public Task OnExecutingAsync(FunctionExecutingContext context, CancellationToken cancellation)
        {
            //activities[context.FunctionInstanceId] = source.StartActivity(context.FunctionName, ActivityKind.Server);
            return Task.CompletedTask;
        }

        public Task OnExecutedAsync(FunctionExecutedContext context, CancellationToken cancellation)
        {
            if (activities.TryRemove(context.FunctionInstanceId, out var activity))
            {
                if (!context.FunctionResult.Succeeded)
                {

                }

                activity?.Stop();
            }

            return Task.CompletedTask;
        }
    }
}
