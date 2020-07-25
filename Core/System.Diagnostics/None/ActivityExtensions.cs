// Polyfill for Activity

using System.Collections.Generic;

namespace System.Diagnostics
{
    public static class Activity2
    {
        internal static Activity CreateAndStart(ActivitySource source, string name, ActivityKind kind, string? parentId, ActivityContext parentContext,
                                                IEnumerable<KeyValuePair<string, object?>>? tags, IEnumerable<ActivityLink>? links,
                                                DateTimeOffset startTime, ActivityDataRequest request)
        {
            Activity activity = new Activity(name);

            //activity.Source = source;
            //activity.Kind = kind;

            if (parentId != null)
            {
                activity.SetParentId(parentId);
            }
            else if (parentContext != default)
            {
                //activity._traceId = parentContext.TraceId.ToString();
                //activity._parentSpanId = parentContext.SpanId.ToString();
                //activity.ActivityTraceFlags = parentContext.TraceFlags;
                //activity._traceState = parentContext.TraceState;
            }
            else
            {
                Activity? parent = Activity.Current;
                if (parent != null)
                {
                    // The parent change should not form a loop. We are actually guaranteed this because
                    // 1. Un-started activities can't be 'Current' (thus can't be 'parent'), we throw if you try.
                    // 2. All started activities have a finite parent change (by inductive reasoning).
                    activity.Parent = parent;
                }
            }

            activity.IdFormat =
                ForceDefaultIdFormat ? DefaultIdFormat :
                activity.Parent != null ? activity.Parent.IdFormat :
                activity._parentSpanId != null ? ActivityIdFormat.W3C :
                activity._parentId == null ? DefaultIdFormat :
                IsW3CId(activity._parentId) ? ActivityIdFormat.W3C :
                ActivityIdFormat.Hierarchical;

            if (activity.IdFormat == ActivityIdFormat.W3C)
                activity.GenerateW3CId();
            else
                activity._id = activity.GenerateHierarchicalId();

            if (links != null)
            {
                using (IEnumerator<ActivityLink> enumerator = links.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        activity._links = new LinkedList<ActivityLink>(enumerator);
                    }
                }
            }

            if (tags != null)
            {
                using (IEnumerator<KeyValuePair<string, object?>> enumerator = tags.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        activity._tags = new TagsLinkedList(enumerator);
                    }
                }
            }

            activity.StartTimeUtc = startTime == default ? DateTime.UtcNow : startTime.DateTime;

            activity.IsAllDataRequested = request == ActivityDataRequest.AllData || request == ActivityDataRequest.AllDataAndRecorded;

            if (request == ActivityDataRequest.AllDataAndRecorded)
            {
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }

            SetCurrent(activity);

            return activity;
        }

    }
}
