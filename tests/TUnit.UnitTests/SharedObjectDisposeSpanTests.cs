#if NET

using System.Collections.Concurrent;
using System.Diagnostics;
using TUnit.Core;
using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.UnitTests;

/// <summary>
/// Disposing a shared data-source object emits a <c>dispose &lt;Type&gt;</c> span parented and tagged
/// like its <c>initialize</c> span, so fixture teardown cost shows in the timeline (#7287).
/// </summary>
public class SharedObjectDisposeSpanTests
{
    [Test]
    public async Task PerClass_Shared_Object_Disposal_Emits_Span_Under_Class_Activity()
    {
        var testContext = CreateContext();
        var fixture = new SpanFixture();
        RegisterScope(fixture, SharedType.PerClass);

        using var scope = new StoppedActivityScope();
        await TrackAndUntrack(testContext, fixture);

        await Assert.That(fixture.IsDisposed).IsTrue();

        var span = scope.Single($"dispose {nameof(SharedObjectDisposeSpanTests)}.{nameof(SpanFixture)}");
        await Assert.That(span.Source.Name).IsEqualTo(TUnitActivitySource.LifecycleSourceName);
        await Assert.That(span.GetTagItem(TUnitActivitySource.TagTraceScope)).IsEqualTo("class");
        await Assert.That(span.ParentSpanId).IsNotEqualTo(default(ActivitySpanId));
        await Assert.That(span.ParentSpanId).IsEqualTo(testContext.ClassContext.Activity!.Context.SpanId);
        await Assert.That(span.GetTagItem(TUnitActivitySource.TagTestClass)).IsEqualTo(testContext.ClassContext.ClassType.FullName);
    }

    [Test]
    public async Task Session_Shared_Object_Disposal_Emits_Span_Under_Assembly_Activity()
    {
        var testContext = CreateContext();
        var fixture = new SpanFixture();
        RegisterScope(fixture, SharedType.PerTestSession);

        using var scope = new StoppedActivityScope();
        await TrackAndUntrack(testContext, fixture);

        var span = scope.Single($"dispose {nameof(SharedObjectDisposeSpanTests)}.{nameof(SpanFixture)}");
        await Assert.That(span.GetTagItem(TUnitActivitySource.TagTraceScope)).IsEqualTo("session");
        await Assert.That(span.ParentSpanId).IsNotEqualTo(default(ActivitySpanId));
        await Assert.That(span.ParentSpanId).IsEqualTo(testContext.ClassContext.AssemblyContext.Activity!.Context.SpanId);
    }

    [Test]
    public async Task Failing_Disposal_Records_Error_On_Span_And_Still_Throws()
    {
        var testContext = CreateContext();
        var fixture = new SpanFixture { Throw = true };
        RegisterScope(fixture, SharedType.PerAssembly);

        using var scope = new StoppedActivityScope();
        var exceptions = await TrackAndUntrack(testContext, fixture);

        await Assert.That(exceptions).IsNotNull();
        var span = scope.Single($"dispose {nameof(SharedObjectDisposeSpanTests)}.{nameof(SpanFixture)}");
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Error);
    }

    [Test]
    public async Task Per_Test_Object_Disposal_Emits_No_Span()
    {
        var testContext = CreateContext();
        var fixture = new SpanFixture();
        RegisterScope(fixture, SharedType.None);

        using var scope = new StoppedActivityScope();
        await TrackAndUntrack(testContext, fixture);

        await Assert.That(fixture.IsDisposed).IsTrue();
        await Assert.That(scope.Activities).IsEmpty();
    }

    private static async Task<List<Exception>?> TrackAndUntrack(TestContext testContext, object obj)
    {
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(new FakeDiscoverer(obj)), GlobalContext.Current.Disposer);
        tracker.TrackObjects(testContext);
        return await tracker.UntrackObjects(testContext);
    }

    private static void RegisterScope(object obj, SharedType sharedType) =>
        TraceScopeRegistry.RegisterFromDataSource(new ClassDataSourceAttribute<SpanFixture> { Shared = sharedType }, [obj]);

    /// <summary>
    /// Builds a test context on its own class and assembly contexts, each with a distinct explicit
    /// activity, so the parent assertions cannot pass by both sides falling back to <c>default</c>
    /// and the running test's shared contexts are left untouched.
    /// </summary>
    private static TestContext CreateContext()
    {
        var currentContext = TestContext.Current!;
        var sessionContext = new TestSessionContext(new TestDiscoveryContext(new BeforeTestDiscoveryContext { TestFilter = null }) { TestFilter = null })
        {
            Id = Guid.NewGuid().ToString(),
            TestFilter = null
        };
        var assemblyContext = new AssemblyHookContext(sessionContext)
        {
            Assembly = typeof(SharedObjectDisposeSpanTests).Assembly,
            Activity = CreateStoppedActivity("test assembly")
        };
        var classContext = new ClassHookContext(assemblyContext)
        {
            ClassType = typeof(SharedObjectDisposeSpanTests),
            Activity = CreateStoppedActivity("test suite")
        };

        return new TestContext(nameof(SharedObjectDisposeSpanTests), currentContext.ServiceProvider, classContext,
            new TestBuilderContext { TestMetadata = currentContext.TestDetails.MethodMetadata }, CancellationToken.None);
    }

    // Started without a listener and stopped at once: only its context is needed as a parent,
    // and stopping restores Activity.Current.
    private static Activity CreateStoppedActivity(string name)
    {
        var activity = new Activity(name).SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.Stop();
        return activity;
    }

    public sealed class SpanFixture : IAsyncDisposable
    {
        public bool Throw { get; init; }

        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return Throw ? throw new InvalidOperationException("dispose failed") : default;
        }
    }

    // Discovery adds the object to the test's tracked set, as the real discoverer does.
    private sealed class FakeDiscoverer(object obj) : IObjectGraphDiscoverer
    {
        public SortedList<int, HashSet<object>> DiscoverAndTrackObjects(TestContext testContext, CancellationToken cancellationToken = default)
        {
            var tracked = testContext.TrackedObjects;
            tracked[0] = new HashSet<object>(TUnit.Core.Helpers.ReferenceEqualityComparer.Instance) { obj };
            return tracked;
        }

        public SortedList<int, HashSet<object>> TrackRootObjects(TestContext testContext)
            => testContext.TrackedObjects;

        public ObjectGraph DiscoverObjectGraph(TestContext testContext, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ObjectGraph DiscoverNestedObjectGraph(object rootObject, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Captures lifecycle spans stopped on this test's async flow only, so spans from tests
    /// running in parallel do not leak in.
    /// </summary>
    private sealed class StoppedActivityScope : IDisposable
    {
        private static readonly AsyncLocal<StoppedActivityScope?> CurrentScope = new();

        private readonly ActivityListener _listener;
        private readonly ConcurrentQueue<Activity> _activities = new();

        public StoppedActivityScope()
        {
            CurrentScope.Value = this;
            _listener = new ActivityListener
            {
                ShouldListenTo = static source => source.Name is TUnitActivitySource.SourceName or TUnitActivitySource.LifecycleSourceName,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (ReferenceEquals(CurrentScope.Value, this) && activity.OperationName.StartsWith("dispose ", StringComparison.Ordinal))
                    {
                        _activities.Enqueue(activity);
                    }
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public IReadOnlyCollection<Activity> Activities => _activities.ToArray();

        public Activity Single(string name) => _activities.Single(a => a.OperationName == name);

        public void Dispose()
        {
            CurrentScope.Value = null;
            _listener.Dispose();
        }
    }
}

#endif
