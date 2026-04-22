#if NET

using System.Diagnostics;

namespace TUnit.Core;

public static class TUnitActivitySource
{
    private static readonly string Version =
        typeof(TUnitActivitySource).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    internal const string SourceName = "TUnit";
    internal const string LifecycleSourceName = "TUnit.Lifecycle";

    /// <summary>
    /// Activity source emitted by TUnit's ASP.NET Core HTTP propagation handlers.
    /// Registered automatically on the SUT's <see cref="System.Diagnostics.ActivitySource"/>
    /// listeners by <c>TestWebApplicationFactory</c>.
    /// </summary>
    public const string AspNetCoreHttpSourceName = "TUnit.AspNetCore.Http";

    /// <summary>
    /// Activity source emitted by TUnit's Aspire HTTP propagation handler.
    /// Registered automatically by <c>TUnit.OpenTelemetry.AutoStart</c> so outbound
    /// requests made through <c>AspireFixture.CreateHttpClient</c> appear as client spans.
    /// </summary>
    public const string AspireHttpSourceName = "TUnit.Aspire.Http";

    /// <summary>W3C baggage HTTP header name.</summary>
    internal const string BaggageHeader = "baggage";

    internal static readonly ActivitySource Source = new(SourceName, Version);
    internal static readonly ActivitySource LifecycleSource = new(LifecycleSourceName, Version);

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
    internal const string TagSessionId = "tunit.session.id";
    internal const string TagTestFilter = "tunit.filter";
    internal const string TagTestClass = "tunit.test.class";
    internal const string TagTestMethod = "tunit.test.method";
    internal const string TagTestNodeUid = "tunit.test.node_uid";
    internal const string TagTestCategories = "tunit.test.categories";
    internal const string TagTestCount = "tunit.test.count";
    internal const string TagClassNamespace = "tunit.class.namespace";
    internal const string TagTestCaseName = "test.case.name";
    internal const string TagTestSuiteName = "test.suite.name";
    internal const string TagAssemblyName = "tunit.assembly.name";
    internal const string TagTestCaseResultStatus = "test.case.result.status";
    internal const string TagTestRetryAttempt = "tunit.test.retry_attempt";
    internal const string TagTestSkipReason = "tunit.test.skip_reason";
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

    internal static Activity? StartLifecycleActivity(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        ActivityContext parentContext = default,
        IEnumerable<KeyValuePair<string, object?>>? tags = null,
        IEnumerable<ActivityLink>? links = null)
    {
        return LifecycleSource.StartActivity(name, kind, parentContext, tags, links);
    }

    internal static ActivitySource GetSourceForSharedType(SharedType? sharedType) =>
        sharedType is not null and not SharedType.None
            ? LifecycleSource
            : Source;

    /// <summary>
    /// Runs <paramref name="action"/> inside an OpenTelemetry span, handling
    /// Activity.Current save/restore, error recording, and span stop/dispose.
    /// </summary>
    internal static Task RunWithSpanAsync(
        string name,
        ActivityContext parentContext,
        IEnumerable<KeyValuePair<string, object?>> tags,
        Func<Task> action)
    {
        return RunWithSpanAsync(Source, name, parentContext, tags, action);
    }

    /// <summary>
    /// Runs <paramref name="action"/> inside an OpenTelemetry span emitted by
    /// <paramref name="activitySource"/>, handling Activity.Current save/restore,
    /// error recording, and span stop/dispose.
    /// </summary>
    internal static async Task RunWithSpanAsync(
        ActivitySource activitySource,
        string name,
        ActivityContext parentContext,
        IEnumerable<KeyValuePair<string, object?>> tags,
        Func<Task> action)
    {
        Activity? activity = null;
        var previousActivity = Activity.Current;

        if (activitySource.HasListeners())
        {
            activity = activitySource.StartActivity(name, ActivityKind.Internal, parentContext, tags);
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

        var exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
        var tagsCollection = new ActivityTagsCollection
        {
            { "exception.type", exceptionType },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.ToString() }
        };

        activity.AddEvent(new ActivityEvent("exception", tags: tagsCollection));
        activity.SetTag("error.type", exceptionType);
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
    /// Detaches the per-test activity from the ambient async-local context so that
    /// subsequent shared (class/assembly/session) cleanup and hook execution do not
    /// inherit the test trace. The test activity itself is kept alive so it can be
    /// finished with its final status and exported.
    /// </summary>
    internal static void DetachTestActivityFromAmbientContext()
    {
        Activity.Current = null;
    }

    /// <summary>
    /// Builds a W3C <c>baggage</c> header value from <paramref name="activity"/>'s baggage,
    /// or returns <c>null</c> if there is nothing to emit. Used by HTTP propagation handlers
    /// when the configured <see cref="DistributedContextPropagator"/> does not emit baggage
    /// itself (e.g. .NET's default LegacyPropagator emits Correlation-Context instead).
    /// </summary>
    internal static string? TryBuildBaggageHeader(Activity activity)
    {
        System.Text.StringBuilder? sb = null;

        foreach (var (key, value) in activity.Baggage)
        {
            if (key is null)
            {
                continue;
            }

            if (sb is null)
            {
                sb = new System.Text.StringBuilder();
            }
            else
            {
                sb.Append(',');
            }

            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value ?? string.Empty));
        }

        return sb?.ToString();
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
