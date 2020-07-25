using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cloudy
{
    [ServiceEnvironment("Development")]
    class DevelopmentEventStream : IEventStream
    {
        readonly HttpClient http;
        readonly ISerializer serializer;
        readonly IServiceProvider services;
        readonly string gridUrl;

        public DevelopmentEventStream(HttpClient http, ISerializer serializer, IEnvironment env, IServiceProvider services)
            => (this.http, this.serializer, gridUrl, this.services)
            = (http, serializer, env.GetVariable("EventGridUrl" , ""), services);

        public async Task PushAsync<TEvent>(TEvent e)
        {
            if (e == null)
                return;

            if (string.IsNullOrEmpty(gridUrl))
            {
                // If doing proper end to end via eventgrid callback is not set up, 
                // call handlers inline, just like we do in the event grid trigger function.
                dynamic dynamicEvt = e;
                ((dynamic)this).HandleAsync(dynamicEvt);

                return;
            }

            var evt = e!.ToEventGrid(serializer);

            using var request = new HttpRequestMessage(HttpMethod.Post, gridUrl);
            request.Content = new StringContent(serializer.Serialize(evt), Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("aeg-event-type", "Notification");

            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        async Task HandleAsync<TEvent>(TEvent e)
        {
            var handlers = (IEnumerable<IEventHandler<TEvent>>)services.GetService(typeof(IEnumerable<IEventHandler<TEvent>>));
            foreach (var handler in handlers.OrderBy(h => h.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0))
            {
                await handler.HandleAsync(e).ConfigureAwait(false);
            }
        }
    }
}
