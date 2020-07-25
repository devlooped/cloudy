namespace System.Diagnostics
{
    /// <summary>
    /// Provides the missing APIs in <see cref="Activity"/> until .NET5 ships.
    /// </summary>
    public class Activity2 : Activity, IDisposable
    {
        static readonly ActivitySource defaultSource = new ActivitySource("");

        public Activity2(string operationName) : base(operationName) 
            => Source = defaultSource;

        public ActivityContext Context => new ActivityContext(TraceId, SpanId, ActivityTraceFlags, TraceStateString);
        public ActivityKind Kind { get; internal set; }
        public ActivitySource Source { get; internal set; }

        public void Dispose()
        {
            Source.NotifyActivityStopping(this);
            Stop();
            Source.NotifyActivityStopped(this);
        }
    }
}
