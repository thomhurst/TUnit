#if NET

using System.Diagnostics;

namespace TUnit.Core;

public static class TUnitActivitySource
{
    private static readonly string Version =
        typeof(TUnitActivitySource).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    internal static readonly ActivitySource Source = new("TUnit", Version);

    // Span names used across the engine and HTML report.
    internal const string SpanTestSession = "test session";
    internal const string SpanTestAssembly = "test assembly";
    internal const string SpanTestSuite = "test suite";
    internal const string SpanTestCase = "test case";
    internal const string SpanTestBody = "test body";

    // Tag and baggage keys used across init/dispose spans, HTML report, and cross-boundary correlation.

    /// <summary>
    /// The key used for the test context ID in both Activity span tags and Activity baggage.
    /// Set as baggage on each test's Activity span to enable automatic
    /// <see cref="TestContext.Current"/> resolution when OpenTelemetry propagation is configured.
    /// </summary>
    public const string TagTestId = "tunit.test.id";
    internal const string TagTestClass = "tunit.test.class";
    internal const string TagTraceScope = "tunit.trace.scope";

    /// <summary>
    /// Returns a human-readable type name suitable for span labels.
    /// Strips the namespace but preserves nesting (Outer.Inner) and cleans up
    /// generic arity suffixes (MySource`1 → MySource&lt;T&gt;).
    /// </summary>
    internal static string GetReadableTypeName(Type type)
    {
        var name = type.FullName ?? type.Name;

        // Strip namespace: take everything after the last '.' that isn't part of nesting
        var nsEnd = name.LastIndexOf('.');
        if (nsEnd >= 0 && type.Namespace is { Length: > 0 })
        {
            name = name[type.Namespace.Length..].TrimStart('.');
        }

        // Replace '+' nesting separator with '.'
        name = name.Replace('+', '.');

        // Clean up generic arity suffixes: MySource`1 → MySource
        var backtick = name.IndexOf('`');
        if (backtick >= 0)
        {
            name = name[..backtick];
        }

        return name;
    }

    internal static Activity? StartActivity(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext parentContext = default,
        IEnumerable<KeyValuePair<string, object?>>? tags = null,
        IEnumerable<ActivityLink>? links = null)
    {
        return Source.StartActivity(name, kind, parentContext, tags, links);
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
            // Activity.Current is AsyncLocal — restore so the ambient context on this
            // execution path is not permanently set to the span we just stopped.
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
