using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    public class ActivitySource
    {
        static readonly ConcurrentDictionary<ActivityListener, ActivityListener> listeners = new ConcurrentDictionary<ActivityListener, ActivityListener>();

        public ActivitySource(string name, string? version = "") 
            => (Name, Version)
            = (name, version);

        public string Name { get; }
        public string? Version { get; }

        public Activity2? StartActivity([CallerMemberName] string operationName = "", ActivityKind kind = ActivityKind.Internal, IEnumerable<KeyValuePair<string, string?>>? tags = null)
            => StartActivityCore(operationName, kind, tags);

        public Activity2? StartActivity(ActivityKind kind, [CallerMemberName] string operationName = "", IEnumerable<KeyValuePair<string, string?>>? tags = null)
            => StartActivityCore(operationName, kind, tags);

        public static void DetachListener(ActivityListener activityListener) 
            => listeners.TryRemove(activityListener, out _);

        public static void AddActivityListener(ActivityListener listener)
            => listeners.TryAdd(listener, listener);

        Activity2 StartActivityCore(string name, ActivityKind kind, IEnumerable<KeyValuePair<string, string?>>? tags)
        {
            var activity = new Activity2(name)
            {
                Kind = kind,
                Source = this,
            };
            
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    if (tag.Value != null)
                        activity.AddTag(tag.Key, tag.Value);
                }
            }
            
            activity.Start();

            foreach (var listener in listeners.Keys.ToArray())
            {
                listener.ActivityStarted?.Invoke(activity);
            }

            return activity;
        }

        internal void NotifyActivityStopping(Activity2 activity)
        {
            foreach (var listener in listeners.Keys.ToArray())
            {
                listener.ActivityStopping?.Invoke(activity);
            }
        }

        internal void NotifyActivityStopped(Activity2 activity)
        {
            foreach (var listener in listeners.Keys.ToArray())
            {
                listener.ActivityStopped?.Invoke(activity);
            }
        }
    }
}
