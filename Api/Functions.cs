using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public Functions(CloudStorageAccount storage, IEventStream events, ISerializer serializer, IServiceProvider services, TelemetryClient telemetry)
            => (this.storage, this.events, this.serializer, this.services, this.telemetry)
            = (storage, events, serializer, services, telemetry);

        [FunctionName("inbox")]
        public async Task<IActionResult> InboxAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            await events.PushAsync(new InboxArrived(req.Query["message"])).ConfigureAwait(false);
            return new OkResult();
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
            await table.ExecuteAsync(TableOperation.Insert(gridEvent.ToEntity())).ConfigureAwait(false);
        }

        async Task HandleAsync<TEvent>(TEvent e)
        {
            var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));
            foreach (var handler in handlers.OrderBy(h => h.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0))
            {
                using var operation = telemetry.StartOperation<RequestTelemetry>(handler.GetType().FullName);
                await handler.HandleAsync(e).ConfigureAwait(false);
            }
        }
    }
}
