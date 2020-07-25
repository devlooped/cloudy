#pragma warning disable CS0618 // For some reason, all Filter APIs are marked obsolete because of potential breaking changes
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudy
{
    [ServiceLifetime(ServiceLifetime.Singleton)]
    class ActivityTelemetry
    {
        static ActivitySource source = new ActivitySource(nameof(Cloudy), typeof(ActivityTelemetry).Assembly.GetName().Version.ToString(2));

        readonly TelemetryClient telemetry;
        readonly ISerializer serializer;
        readonly ActivityListener listener;
        readonly ConcurrentDictionary<Activity, IDisposable> operations = new ConcurrentDictionary<Activity, IDisposable>();

        static  ActivityTelemetry()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
        }

        public ActivityTelemetry(TelemetryClient telemetry, ISerializer serializer)
        {
            this.telemetry = telemetry;
            this.serializer = serializer;
            listener = new ActivityListener
            {
                ActivityStarted = OnActivityStarted,
                ActivityStopping = OnActivityStopping,
                ActivityStopped = OnActivityStopped,
                //ShouldListenTo = s => s == source,
                //GetRequestedDataUsingContext = OnGetRequestedData,
                //GetRequestedDataUsingParentId = OnGetRequestedData,
            };

            ActivitySource.AddActivityListener(listener);
        }

        public ActivitySource ActivitySource => source;

        void OnActivityStarted(Activity2 activity)
        {
            OperationTelemetry? operation = default;

            if (activity.Kind == ActivityKind.Server)
            {
                var holder = telemetry.StartOperation<RequestTelemetry>(activity);
                if (activity.TryGetTag("Source", out var source))
                    holder.Telemetry.Source = source;

                operation = holder.Telemetry;
                operations[activity] = holder;
            }
            else if (activity.Kind == ActivityKind.Client)
            {
                var holder = telemetry.StartOperation<DependencyTelemetry>(activity);
                if (activity.TryGetTag("Dependency", out var type))
                    holder.Telemetry.Type = type;
                if (activity.TryGetTag("Target", out var target))
                    holder.Telemetry.Target = target;

                operation = holder.Telemetry;
                operations[activity] = holder;
            }

            if (operation != null)
            {
                File.AppendAllText(@"C:\Temp\activity.log", serializer.Serialize(activity) + System.Environment.NewLine);
                File.AppendAllText(@"C:\Temp\operation.log", serializer.Serialize(operation) + System.Environment.NewLine);
            }

            // NOTE: ActivityKind.Consumer|Producer|Internal, what should those report to telemetry?
        }

        void OnActivityStopping(Activity2 activity)
        {
            if (operations.TryRemove(activity, out var operation))
            {
                operation.Dispose();
#if DEBUG
                telemetry.Flush();
#endif
            }
        }

        void OnActivityStopped(Activity2 activity)
        {
        }

        //ActivityDataRequest OnGetRequestedData(ref ActivityCreationOptions<ActivityContext> options) => ActivityDataRequest.AllData;

        //ActivityDataRequest OnGetRequestedData(ref ActivityCreationOptions<string> options) => ActivityDataRequest.AllData;

    }
}
