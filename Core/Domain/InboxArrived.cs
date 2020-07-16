namespace Cloudy
{
    public class InboxArrived
    {
        public InboxArrived(string message) => Message = message;

        public string Message { get; }
    }
}
