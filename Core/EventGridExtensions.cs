using System;
using System.Globalization;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;

namespace Cloudy
{
    static class EventGridExtensions
    {
        public static object GetData(this EventGridEvent e, ISerializer serializer) 
            => serializer.Deserialize(e.Data.ToString()!, Type.GetType(e.EventType, true)!);

        public static EventGridEvent ToEventGrid(this object data, ISerializer serializer)
        {
            var metadata = data as IEventMetadata;
            var now = PreciseTime.UtcNow;

            return new EventGridEvent
            {
                Id = metadata?.EventId ?? now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("n"),
                EventType = data.GetType().FullName!,
                EventTime = metadata?.EventTime ?? now,
                Data = serializer.Serialize(data),
                DataVersion = data.GetType().Assembly.GetName().Version?.ToString(2) ?? "1.0",
                // Subject is required, so provide a default if not set
                Subject = metadata?.Subject ?? data.GetType().Namespace,
                // Unless the object itself provides a different default, 
                // like DomainEvent does, we send everything to the Default topic
                Topic = metadata?.Topic ?? "Default",
            };
        }

        public static TableEntity ToEntity(this EventGridEvent e)
        {
            // The actual topic contains a gigantic amount of jargon with the format:
            // /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventGrid/domains/{domainName}/topics/{topicName}
            // The only useful bit worth persisting is the actual topic at the end, which we could find a use for.
            var topic = e.Topic;
            var index = e.Topic.IndexOf("/topics/", StringComparison.Ordinal);
            if (index != -1)
                topic = e.Topic.Substring(index + 8);

            return new EventGridEventEntity
            {
                PartitionKey = e.EventType,
                RowKey = e.Id,
                Data = e.Data.ToString(),
                DataVersion = e.DataVersion,
                Subject = e.Subject,
                Topic = topic,
            };
        }

        class EventGridEventEntity : TableEntity
        {
            public string? Data { get; set; }
            public string? DataVersion { get; set; }
            public string? Subject { get; set; }
            public string? Topic { get; set; }
        }
    }
}
