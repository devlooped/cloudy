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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cloudy
{
    internal class Functions
    {
        static readonly StringBuilder EmptyLog = new StringBuilder();

        readonly CloudStorageAccount storage;
        readonly IEventStream events;
        readonly ISerializer serializer;
        readonly IServiceProvider services;
        readonly ILogger<Functions> logger;
        readonly IHttpContextAccessor context;

        public Functions(CloudStorageAccount storage, IEventStream events, ISerializer serializer, IServiceProvider services, ILogger<Functions> logger, IHttpContextAccessor context)
            => (this.storage, this.events, this.serializer, this.services, this.logger, this.context)
            = (storage, events, serializer, services, logger, context);

        [FunctionName("inbox")]
        public async Task<IActionResult> InboxAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            if (req.Query.ContainsKey("throw"))
                throw new ArgumentException(req.Query["throw"]);

            var log = Log(req);

            await events.PushAsync(new InboxArrived(req.Query["message"])).ConfigureAwait(false);

            return new OkObjectResult(log.ToString());
        }

        [FunctionName("event-handle")]
        public Task HandleAsync([EventGridTrigger] EventGridEvent gridEvent)
        {
            Log(context.HttpContext.Request);

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
            var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));
            foreach (var handler in handlers.OrderBy(h => h.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0))
            {
                await handler.HandleAsync(e).ConfigureAwait(false);
            }
        }

        StringBuilder Log(HttpRequest request)
        {
#if !DEBUG
            return EmptyLog;
#else
            var builder = new StringBuilder();
            builder.AppendLine(request.GetDisplayUrl());
            foreach (var header in request.Headers)
            {
                builder.Append(header.Key).Append(": ").AppendLine(header.Value);
            }
            builder.AppendLine();

            var activity = Activity.Current;
            if (activity != null)
            {
                builder.Append($"Activity.{nameof(Activity.Id)}: ").AppendLine(activity.Id);
                builder.Append($"Activity.{nameof(Activity.TraceId)}: ").AppendLine(activity.TraceId.ToString());
                builder.Append($"Activity.{nameof(Activity.RootId)}: ").AppendLine(activity.RootId);
                builder.Append($"Activity.{nameof(Activity.ParentId)}: ").AppendLine(activity.ParentId);
                builder.Append($"Activity.{nameof(Activity.ParentSpanId)}: ").AppendLine(activity.ParentSpanId.ToString());
                builder.Append($"Activity.{nameof(Activity.SpanId)}: ").AppendLine(activity.SpanId.ToString());
            }

            logger.LogInformation(builder.ToString());

            return builder;
#endif
        }
    }
}
