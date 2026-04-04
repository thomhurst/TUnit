#if NET

using System.Diagnostics;

namespace TUnit.Core;

internal static class TUnitActivitySource
{
    private static readonly string Version =
        typeof(TUnitActivitySource).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    internal static readonly ActivitySource Source = new("TUnit", Version);

    internal static Activity? StartActivity(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext parentContext = default,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        // StartActivity returns null when no listener is sampling this source,
        // so the HasListeners() check is implicit. We rely on the framework behavior.
        return Source.StartActivity(name, kind, parentContext, tags);
    }

    internal static void RecordException(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        var tagsCollection = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.ToString() }
        };

        activity.AddEvent(new ActivityEvent("exception", tags: tagsCollection));
        activity.SetTag("error.type", exception.GetType().FullName);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }

    internal static void StopActivity(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.Stop();
        activity.Dispose();
    }

    /// <summary>
    /// Maps a <see cref="SharedType"/> to its trace scope tag value.
    /// Keyed objects map to "session" because they are parented under the session activity
    /// (shared across classes and assemblies via matching keys).
    /// </summary>
    internal static string GetScopeTag(SharedType? sharedType) => sharedType switch
    {
        SharedType.PerTestSession => "session",
        SharedType.PerAssembly => "assembly",
        SharedType.PerClass => "class",
        SharedType.Keyed => "session",
        _ => "test"
    };
}

#endif
