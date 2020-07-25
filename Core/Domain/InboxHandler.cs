using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Cloudy
{
    class InboxHandler : IEventHandler<InboxArrived>
    {
        readonly IEventStream events;
        readonly ILogger<InboxHandler> logger;

        public InboxHandler(IEventStream events, ILogger<InboxHandler> logger)
            => (this.events, this.logger)
            = (events, logger);

        public async Task HandleAsync(InboxArrived e)
        {
            logger.LogInformation("Inbox: {0}", e.Message);
            await events.PushAsync(new MessageProcessed(e.Message));
        }
    }
}
