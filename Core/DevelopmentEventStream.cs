using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cloudy
{
    [ServiceEnvironment("Development")]
    class DevelopmentEventStream : IEventStream
    {
        readonly HttpClient http;
        readonly ISerializer serializer;
        readonly ActivitySource source;
        readonly string gridUrl;

        public DevelopmentEventStream(HttpClient http, ISerializer serializer, IEnvironment env, ActivitySource source)
            => (this.http, this.serializer, this.source, gridUrl)
            = (http, serializer, source, env.GetVariable("EventGridUrl"));

        public async Task PushAsync<TEvent>(TEvent e)
        {
            using var activity = source.StartActivity(typeof(TEvent).Name, ActivityKind.Client, 
                KeyValuePair.Create("Dependency", "EventGrid"), 
                KeyValuePair.Create("Target", typeof(TEvent).Name));

            var evt = e!.ToEventGrid(serializer);

            using var request = new HttpRequestMessage(HttpMethod.Post, gridUrl);
            request.Content = new StringContent(serializer.Serialize(evt), Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("aeg-event-type", "Notification");

            using var response = await http.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }
    }
}
