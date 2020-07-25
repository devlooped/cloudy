namespace Cloudy
{
    public class MessageProcessed
    {
        public MessageProcessed(string message) => Message = message;

        public string Message { get; }
    }
}
