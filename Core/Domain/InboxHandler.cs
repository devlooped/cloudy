using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Cloudy
{
    class InboxHandler : IEventHandler<InboxArrived>
    {
        readonly IEventStream events;
        readonly ActivitySource source;
        readonly ILogger<InboxHandler> logger;

        public InboxHandler(IEventStream events, ActivitySource source, ILogger<InboxHandler> logger) 
            => (this.events, this.source, this.logger)
            = (events, source, logger);

        public async Task HandleAsync(InboxArrived e)
        {
            logger.LogInformation("Inbox: {0}", e.Message);
            await events.PushAsync(new MessageProcessed(e.Message));
        }
    }
}
