#if NET

using System.Diagnostics;

namespace TUnit.Core;

internal static class TUnitActivitySource
{
    private static readonly string Version =
        typeof(TUnitActivitySource).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    internal static readonly ActivitySource Source = new("TUnit", Version);

    // Tag keys used across init/dispose spans and the HTML report.
    internal const string TagTestId = "tunit.test.id";
    internal const string TagTestClass = "tunit.test.class";
    internal const string TagTraceScope = "tunit.trace.scope";

    internal static Activity? StartActivity(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext parentContext = default,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        return Source.StartActivity(name, kind, parentContext, tags);
    }

    /// <summary>
    /// Runs <paramref name="action"/> inside an OpenTelemetry span, handling
    /// Activity.Current save/restore, error recording, and span stop/dispose.
    /// </summary>
    internal static async Task RunWithSpanAsync(
        string name,
        ActivityContext parentContext,
        IEnumerable<KeyValuePair<string, object?>> tags,
        Func<Task> action)
    {
        Activity? activity = null;
        var previousActivity = Activity.Current;

        if (Source.HasListeners())
        {
            activity = Source.StartActivity(name, ActivityKind.Internal, parentContext, tags);
            if (activity is not null)
            {
                Activity.Current = activity;
            }
        }

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            throw;
        }
        finally
        {
            // Activity.Current is thread-static; this restore only affects the current thread.
            // Async continuations on other threads will have already captured their own value.
            StopActivity(activity);
            Activity.Current = previousActivity;
        }
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
    /// Keyed objects map to "session" because their lifetime is session-scoped
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
