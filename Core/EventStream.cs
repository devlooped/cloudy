using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloudy
{
    [ServiceEnvironment("Production")]
    class EventStream : IEventStream
    {
        readonly Uri gridUri;
        readonly string gridKey;
        readonly ISerializer serializer;

        public EventStream(IEnvironment env, ISerializer serializer)
        {
            this.serializer = serializer;
            gridUri = new Uri(env.GetVariable("EventGridUrl"));
            gridKey = env.GetVariable("EventGridAccessKey");
        }

        public async Task PushAsync<TEvent>(TEvent e)
        {
            var credentials = new TopicCredentials(gridKey);
            var domain = gridUri.Host;
            using var client = new EventGridClient(credentials);

            // NOTE: it may not be optimal to push events one by one, but 
            // within a given operation, we don't expect to generate large 
            // numbers of events anyway, even from domain objects...
            await client.PublishEventsAsync(domain, new List<EventGridEvent> { e!.ToEventGrid(serializer) });
        }
    }

    interface IEventStream
    {
        /// <summary>
        /// Pushes an event to the stream, causing any subscriber to be invoked if appropriate.
        /// </summary>
        Task PushAsync<TEvent>(TEvent e);
    }

    /// <summary>
    /// Marker interface so that all handlers can be imported automatically without specifying the type of event.
    /// </summary>
    interface IEventHandler { }

    interface IEventHandler<TEvent> : IEventHandler
    {
        /// <summary>
        /// Handles the event.
        /// </summary>
        Task HandleAsync(TEvent e);
    }

    /// <summary>
    /// Interface that allows events to optionally provide 
    /// custom event serialization metadata.
    /// </summary>
    /// <remarks>
    /// Even though some properties are declared as nullable, 
    /// none of them can be <see langword="null"/> by the time the 
    /// event is pushed to Event Grid.
    /// </remarks>
    interface IEventMetadata
    {
        /// <summary>
        /// A globally unique identifier for the event.
        /// </summary>
        string EventId { get; }

        /// <summary>
        /// The time of the occurrence of the event, typically in UTC.
        /// </summary>
        DateTime EventTime { get; }

        /// <summary>
        /// Typically, the identifier of the domain object that raised 
        /// the event, or some other source/sender identifier.
        /// </summary>
        string? Subject { get; set; }

        /// <summary>
        /// Typically either <c>Domain</c> (for event-sourced events coming 
        /// from a <see cref="DomainObject"/>) or some other category like 
        /// <c>Default</c> for other event areas.
        /// </summary>
        string? Topic { get; }
    }
}
