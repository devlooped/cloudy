using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cloudy
{
    class InboxHandler : IEventHandler<InboxArrived>
    {
        readonly ILogger<InboxHandler> logger;

        public InboxHandler(ILogger<InboxHandler> logger) => this.logger = logger;

        public Task HandleAsync(InboxArrived e)
        {
            logger.LogInformation("Inbox: {0}", e.Message);
            return Task.CompletedTask;
        }
    }
}
