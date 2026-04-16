#if NET

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.Core.Services;
using TUnit.Engine;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for the session activity lifecycle, verifying that the "test session"
/// activity is created at the right time so discovery and execution spans
/// can parent under it — fixing orphaned root spans in traces.
/// See https://github.com/thomhurst/TUnit/issues/5244
/// </summary>
public class SessionActivityLifecycleTests
{
    /// <summary>
    /// Creates a minimal <see cref="HookExecutor"/> with stubbed dependencies.
    /// Only <see cref="IContextProvider.TestSessionContext"/> is used by
    /// <see cref="HookExecutor.TryStartSessionActivity"/>; the rest are
    /// no-op stubs that will never be called.
    /// </summary>
    private static (HookExecutor Executor, TestSessionContext SessionContext) CreateHookExecutor(
        string? testFilter = null)
    {
        var beforeDiscovery = new BeforeTestDiscoveryContext { TestFilter = testFilter };
        var discoveryContext = new TestDiscoveryContext(beforeDiscovery) { TestFilter = testFilter };
        var sessionContext = new TestSessionContext(discoveryContext)
        {
            Id = Guid.NewGuid().ToString(),
            TestFilter = testFilter
        };

        var contextProvider = new StubContextProvider(sessionContext);
        var hookDelegateBuilder = new StubHookDelegateBuilder();

        // EventReceiverOrchestrator is sealed and not used by TryStartSessionActivity,
        // so we pass null — the executor will never call it in these tests.
        var executor = new HookExecutor(hookDelegateBuilder, contextProvider, null!);

        return (executor, sessionContext);
    }

    [Test]
    public async Task TryStartSessionActivity_WithListeners_CreatesSessionActivity()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();

        await Assert.That(sessionContext.Activity).IsNotNull();
        await Assert.That(sessionContext.Activity!.OperationName).IsEqualTo("test session");
        await Assert.That(sessionContext.Activity.Kind).IsEqualTo(ActivityKind.Internal);
    }

    [Test]
    public async Task TryStartSessionActivity_CreatesActivityWithCorrectSource()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        // TUnit's own HTML reporter listener is active during test execution,
        // so HasListeners() is always true here.
        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();

        await Assert.That(sessionContext.Activity).IsNotNull();
        await Assert.That(sessionContext.Activity!.Source.Name).IsEqualTo(TUnitActivitySource.LifecycleSourceName);
    }

    [Test]
    public async Task TryStartSessionActivity_CalledTwice_IsIdempotent()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();
        var firstActivity = sessionContext.Activity;

        executor.TryStartSessionActivity();
        var secondActivity = sessionContext.Activity;

        await Assert.That(firstActivity).IsNotNull();
        await Assert.That(secondActivity).IsSameReferenceAs(firstActivity!);
    }

    [Test]
    public async Task TryStartSessionActivity_SetsSessionIdTag()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();

        var tags = sessionContext.Activity!.Tags.ToList();
        await Assert.That(tags).Contains(
            new KeyValuePair<string, string?>(TUnitActivitySource.TagSessionId, sessionContext.Id));
    }

    [Test]
    public async Task TryStartSessionActivity_SetsTestFilterTag()
    {
        var (executor, sessionContext) = CreateHookExecutor(testFilter: "/*/*/MyClass/*");

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();

        var tags = sessionContext.Activity!.Tags.ToList();
        await Assert.That(tags).Contains(
            new KeyValuePair<string, string?>(TUnitActivitySource.TagTestFilter, "/*/*/MyClass/*"));
    }

    [Test]
    public async Task DiscoveryAndAssembly_ShareSameTrace_WhenBothParentedUnderSession()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();
        var sessionActivity = sessionContext.Activity!;

        // Discovery and assembly spans both parent under the session activity
        using var discoveryActivity = TUnitActivitySource.StartLifecycleActivity(
            "test discovery",
            ActivityKind.Internal,
            sessionActivity.Context);

        using var assemblyActivity = TUnitActivitySource.StartLifecycleActivity(
            TUnitActivitySource.SpanTestAssembly,
            ActivityKind.Internal,
            sessionActivity.Context,
            [new(TUnitActivitySource.TagAssemblyName, "TestAssembly")]);

        // Both should be in the same trace (single unified trace — the fix for #5244)
        await Assert.That(discoveryActivity).IsNotNull();
        await Assert.That(assemblyActivity).IsNotNull();
        await Assert.That(discoveryActivity!.TraceId).IsEqualTo(sessionActivity.TraceId);
        await Assert.That(assemblyActivity!.TraceId).IsEqualTo(sessionActivity.TraceId);

        // Both should be direct children of the session
        await Assert.That(discoveryActivity.ParentSpanId).IsEqualTo(sessionActivity.SpanId);
        await Assert.That(assemblyActivity.ParentSpanId).IsEqualTo(sessionActivity.SpanId);
    }

    [Test]
    public async Task LifecycleHierarchy_UsesSessionTrace_AndTestStartsSeparateTrace()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();
        var sessionActivity = sessionContext.Activity!;

        // Discovery span
        var discoveryActivity = TUnitActivitySource.StartLifecycleActivity(
            "test discovery",
            ActivityKind.Internal,
            sessionActivity.Context);

        // Stop discovery before execution (mirrors real flow)
        TUnitActivitySource.StopActivity(discoveryActivity);

        // Assembly span
        var assemblyActivity = TUnitActivitySource.StartLifecycleActivity(
            TUnitActivitySource.SpanTestAssembly,
            ActivityKind.Internal,
            sessionActivity.Context,
            [new(TUnitActivitySource.TagAssemblyName, "TestAssembly")]);

        // Class span
        var classActivity = TUnitActivitySource.StartLifecycleActivity(
            TUnitActivitySource.SpanTestSuite,
            ActivityKind.Internal,
            assemblyActivity?.Context ?? default,
            [new(TUnitActivitySource.TagTestSuiteName, "TestClass")]);

        // Test spans run on the separate TUnit source and start a new trace per test.
        Activity.Current = null;
        var testActivity = TUnitActivitySource.StartActivity(
            TUnitActivitySource.SpanTestCase,
            ActivityKind.Internal,
            default,
            [new(TUnitActivitySource.TagTestCaseName, "TestMethod")]);

        // Verify hierarchy: session → discovery
        await Assert.That(discoveryActivity).IsNotNull();
        await Assert.That(discoveryActivity!.ParentId).IsEqualTo(sessionActivity.Id);

        // Verify lifecycle hierarchy: session → assembly → class
        await Assert.That(assemblyActivity).IsNotNull();
        await Assert.That(assemblyActivity!.ParentId).IsEqualTo(sessionActivity.Id);

        await Assert.That(classActivity).IsNotNull();
        await Assert.That(classActivity!.ParentId).IsEqualTo(assemblyActivity.Id);

        // Test trace is separate so downstream spans/logs stay isolated per test.
        await Assert.That(testActivity).IsNotNull();
        await Assert.That(testActivity!.Parent).IsNull();
        await Assert.That(testActivity.ParentSpanId).IsEqualTo(default(ActivitySpanId));
        await Assert.That(testActivity.TraceId).IsNotEqualTo(classActivity!.TraceId);

        // Cleanup
        TUnitActivitySource.StopActivity(testActivity);
        TUnitActivitySource.StopActivity(classActivity);
        TUnitActivitySource.StopActivity(assemblyActivity);
    }

    [Test]
    public async Task DiscoverySpan_InSameTrace_WhenParentedUnderSession()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();
        var sessionActivity = sessionContext.Activity!;

        // Create a discovery span parented under session
        using var discoveryActivity = TUnitActivitySource.StartLifecycleActivity(
            "test discovery",
            ActivityKind.Internal,
            sessionActivity.Context);

        // Both spans should share the same trace ID
        await Assert.That(discoveryActivity).IsNotNull();
        await Assert.That(discoveryActivity!.TraceId).IsEqualTo(sessionActivity.TraceId);
        await Assert.That(discoveryActivity.ParentSpanId).IsEqualTo(sessionActivity.SpanId);
    }

    [Test]
    public async Task SessionActivity_StoppedAndCleared_AfterFinish()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();
        var activity = sessionContext.Activity!;

        // Verify activity is running
        await Assert.That(activity.IsStopped).IsFalse();

        // Stop via the same pattern used in ExecuteAfterTestSessionHooksAsync
        TUnitActivitySource.StopActivity(activity);
        sessionContext.Activity = null;

        await Assert.That(activity.IsStopped).IsTrue();
        await Assert.That(sessionContext.Activity).IsNull();
    }

    [Test]
    public async Task HookSpan_ParentsUnderSession_WhenSessionActivityExists()
    {
        var (executor, sessionContext) = CreateHookExecutor();

        using var scope = new ActivityListenerScope();

        executor.TryStartSessionActivity();
        var sessionActivity = sessionContext.Activity!;

        // Simulate a Before(TestSession) hook span — these use context.Activity as parent
        using var hookActivity = TUnitActivitySource.StartActivity(
            "BeforeTestSession: SetupDatabase",
            ActivityKind.Internal,
            sessionActivity.Context);

        await Assert.That(hookActivity).IsNotNull();
        await Assert.That(hookActivity!.ParentId).IsEqualTo(sessionActivity.Id);
    }

    #region Stubs

    /// <summary>
    /// Minimal <see cref="IContextProvider"/> stub that returns a fixed
    /// <see cref="TestSessionContext"/>. Other members throw if called.
    /// </summary>
    private sealed class StubContextProvider(TestSessionContext sessionContext) : IContextProvider
    {
        public BeforeTestDiscoveryContext BeforeTestDiscoveryContext =>
            throw new NotSupportedException();

        public TestDiscoveryContext TestDiscoveryContext =>
            throw new NotSupportedException();

        public TestSessionContext TestSessionContext => sessionContext;

        public AssemblyHookContext GetOrCreateAssemblyContext(Assembly assembly) =>
            throw new NotSupportedException();

        public ClassHookContext GetOrCreateClassContext(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                        DynamicallyAccessedMemberTypes.PublicProperties |
                                        DynamicallyAccessedMemberTypes.PublicMethods)]
            Type classType) =>
            throw new NotSupportedException();

        public TestContext CreateTestContext(
            string testName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                        DynamicallyAccessedMemberTypes.PublicProperties |
                                        DynamicallyAccessedMemberTypes.PublicMethods)]
            Type classType,
            TestBuilderContext testBuilderContext,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// No-op <see cref="IHookDelegateBuilder"/> stub. All collection methods
    /// return empty lists — no hooks are registered in these unit tests.
    /// </summary>
    private sealed class StubHookDelegateBuilder : IHookDelegateBuilder
    {
        public ValueTask InitializeAsync() => default;

        public ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectBeforeTestHooksAsync(Type testClassType) =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectAfterTestHooksAsync(Type testClassType) =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectBeforeEveryTestHooksAsync(Type testClassType) =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<TestContext>>> CollectAfterEveryTestHooksAsync(Type testClassType) =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectBeforeClassHooksAsync(Type testClassType) =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectAfterClassHooksAsync(Type testClassType) =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectBeforeEveryClassHooksAsync() =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<ClassHookContext>>> CollectAfterEveryClassHooksAsync() =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectBeforeAssemblyHooksAsync(Assembly assembly) =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectAfterAssemblyHooksAsync(Assembly assembly) =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectBeforeEveryAssemblyHooksAsync() =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<AssemblyHookContext>>> CollectAfterEveryAssemblyHooksAsync() =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> CollectBeforeTestSessionHooksAsync() =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<TestSessionContext>>> CollectAfterTestSessionHooksAsync() =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<BeforeTestDiscoveryContext>>> CollectBeforeTestDiscoveryHooksAsync() =>
            new([]);
        public ValueTask<IReadOnlyList<NamedHookDelegate<TestDiscoveryContext>>> CollectAfterTestDiscoveryHooksAsync() =>
            new([]);
    }

    #endregion

    #region Activity listener helper

    /// <summary>
    /// Manages an <see cref="ActivityListener"/> scoped to a test, ensuring
    /// cleanup even if the test fails.
    /// </summary>
    private sealed class ActivityListenerScope : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly ConcurrentBag<Activity> _activities = [];

        public ActivityListenerScope()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = static source => source.Name is TUnitActivitySource.SourceName or TUnitActivitySource.LifecycleSourceName,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => _activities.Add(activity),
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            // Stop any activities that are still running to prevent leaks
            foreach (var activity in _activities)
            {
                if (!activity.IsStopped)
                {
                    activity.Stop();
                    activity.Dispose();
                }
            }

            _listener.Dispose();
        }
    }

    #endregion
}

#endif
