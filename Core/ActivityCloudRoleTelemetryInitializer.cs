using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudy
{
    [ServiceLifetime(ServiceLifetime.Singleton)]
    class ActivityTelemetryProcessor : ITelemetryProcessor
    {
        readonly ISerializer serializer;

        public ActivityTelemetryProcessor(ISerializer serializer) => this.serializer = serializer;

        public void Process(ITelemetry item)
        {
            if (item is ISupportProperties props)
            {
                Console.WriteLine(serializer.Serialize(props.Properties));
            }
        }
    }

    [ServiceLifetime(ServiceLifetime.Singleton)]
    class ActivityCloudRoleTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            var activity = Activity.Current;
            if (activity == null)
                return;

            if (activity.TryGetTag("CloudRole", out var cloudRole) && cloudRole != null)
            {
                telemetry.Context.Cloud.RoleName = cloudRole;
                // CloudRole should propagate until overwritten
                activity.AddBaggage("CloudRole", cloudRole);
            }
        }
    }
}
