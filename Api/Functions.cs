using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudy
{
    internal class Functions
    {
        readonly CloudStorageAccount storage;
        readonly IEventStream events;
        readonly ISerializer serializer;
        readonly IServiceProvider services;
        readonly TelemetryClient telemetry;
        readonly ActivitySource activitySource;

        public Functions(
            CloudStorageAccount storage, IEventStream events, ISerializer serializer, 
            IServiceProvider services, TelemetryClient telemetry, ActivitySource activitySource)
            => (this.storage, this.events, this.serializer, this.services, this.telemetry, this.activitySource)
            = (storage, events, serializer, services, telemetry, activitySource);

        [FunctionName("inbox")]
        public async Task<IActionResult> InboxAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            if (req.Query.ContainsKey("throw"))
                throw new ArgumentException(req.Query["throw"]);

            var activity = Activity.Current;
            activity.AddBaggage("Source", "Inbox");

            //using var activity = activitySource.StartActivity("Chota", ActivityKind.Client);
            await events.PushAsync(new InboxArrived(req.Query["message"])).ConfigureAwait(false);

            var builder = new StringBuilder();
            builder.AppendLine(req.GetDisplayUrl());
            foreach (var header in req.Headers)
            {
                builder.Append(header.Key).Append(": ").AppendLine(header.Value);
            }
            builder.AppendLine();

            builder.Append($"Activity.{nameof(Activity.Id)}: ").AppendLine(activity?.Id);
            builder.Append($"Activity.{nameof(Activity.ParentId)}: ").AppendLine(activity?.ParentId);
            builder.Append($"Activity.{nameof(Activity.ParentSpanId)}: ").AppendLine(activity?.ParentSpanId.ToString());
            builder.Append($"Activity.{nameof(Activity.RootId)}: ").AppendLine(activity?.RootId);
            builder.Append($"Activity.{nameof(Activity.SpanId)}: ").AppendLine(activity?.SpanId.ToString());
            builder.Append($"Activity.{nameof(Activity.TraceId)}: ").AppendLine(activity?.TraceId.ToString());

            return new OkObjectResult(builder.ToString());
        }

        [FunctionName("event-handle")]
        public Task HandleAsync([EventGridTrigger] EventGridEvent gridEvent)
        {
            dynamic e = gridEvent.GetData(serializer);
            return ((dynamic)this).HandleAsync(e);
        }

        [FunctionName("event-store")]
        public async Task StoreAsync([EventGridTrigger] EventGridEvent gridEvent)
        {
            var client = storage.CreateCloudTableClient();
            var table = client.GetTableReference("Event");

            await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            await table.ExecuteAsync(TableOperation.InsertOrReplace(gridEvent.ToEntity())).ConfigureAwait(false);
        }

        async Task HandleAsync<TEvent>(TEvent e)
        {
            using var _ = activitySource.StartActivity(typeof(TEvent).Name, ActivityKind.Server, 
                KeyValuePair.Create("CloudRole", typeof(TEvent).Name));

            var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));
            foreach (var handler in handlers.OrderBy(h => h.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0))
            {
                using var __ = activitySource.StartActivity(handler.GetType().Name, ActivityKind.Client, 
                    KeyValuePair.Create("CloudRole", handler.GetType().Name), 
                    KeyValuePair.Create("Dependency", "EventStream"),
                    KeyValuePair.Create("Source", typeof(TEvent).Name),
                    KeyValuePair.Create("Target", handler.GetType().Name));

                using var ___ = activitySource.StartActivity(handler.GetType().Name, ActivityKind.Server, 
                    KeyValuePair.Create("CloudRole", handler.GetType().Name),
                    KeyValuePair.Create("Source", handler.GetType().Name));

                await handler.HandleAsync(e).ConfigureAwait(false);
            }
        }
    }
}
