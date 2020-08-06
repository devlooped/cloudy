using System;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace Cloudy
{
    [SkipServiceConvention]
    public abstract class DomainEvent : IEventMetadata
    {
        DateTime eventTime = PreciseTime.UtcNow;
        string eventWhen;
        string? eventId;
        string? subject;

        protected DomainEvent() => eventWhen = eventTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        DateTime IEventMetadata.EventTime => eventTime;

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        string? IEventMetadata.Subject 
        {
            get => subject;
            set
            {
                if (subject != value)
                {
                    if (subject != null)
                        throw new InvalidOperationException("Event already owned by a different subject.");

                    subject = value ?? throw new ArgumentNullException(nameof(value));
                    eventId = eventWhen + "_" + subject;
                }
            }
        }

        /// <devdoc>
        /// When surfacing the <see cref="EventId"/> for use outside the owning 
        /// <see cref="DomainObject"/>, we must ensure that the identifier is globally 
        /// unique. Since it's based on a (however precise) timing, we add the domain 
        /// object identifier as a suffix, which would guarantee uniqueness since it's 
        /// impossible to genenerate two identical identifiers in a single process 
        /// by using our <see cref="PreciseTime"/> (there's a unit test for that). And 
        /// it's highly unlikely that the same domain object will be processed simultaneously 
        /// and generate a new event at the exact same time in two processes or machines
        /// within a 10th of a microsecond of another.
        /// </devdoc>
        string IEventMetadata.EventId => eventId ?? throw new InvalidOperationException("Subject has not been assigned. Cannot get event identifier.");

        /// <summary>
        /// The other type of event is a System-generated one, outside of a <see cref="DomainObject"/>.
        /// </summary>
        string? IEventMetadata.Topic => "Domain";
    }
}
