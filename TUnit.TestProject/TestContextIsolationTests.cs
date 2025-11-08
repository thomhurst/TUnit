using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Tests to verify that TestContext.Current properly isolates context between tests
/// and doesn't leak context across parallel or sequential test executions.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class TestContextIsolationTests
{
    private static readonly ConcurrentDictionary<string, TestContext?> CapturedContexts = new();
    private static readonly ConcurrentDictionary<string, string> TestIdToTestName = new();
    private static readonly AsyncLocal<string> TestLocalValue = new();
    private static readonly Random RandomInstance = new Random();

    [Before(Test)]
    public void BeforeEachTest(TestContext context)
    {
        // Set a unique value for this test
        var testId = Guid.NewGuid().ToString();
        TestLocalValue.Value = testId;

        // CRITICAL: Capture AsyncLocal values so they flow to the test
        context.AddAsyncLocalValues();

        // Store mapping for later verification
        TestIdToTestName[testId] = context.Metadata.TestDetails.TestName;

        // Add to context for verification in test
        context.StateBag.Items["TestLocalId"] = testId;
        context.StateBag.Items["TestStartThread"] = Thread.CurrentThread.ManagedThreadId;
    }

    [Test]
    [Repeat(5)] // Run multiple times to increase chance of catching issues
    public async Task TestContext_Should_Be_Isolated_In_Parallel_Test1()
    {
        var context = TestContext.Current;
        await Assert.That(context).IsNotNull();

        var testId = context!.StateBag.Items["TestLocalId"] as string;
        await Assert.That(testId).IsNotNull();

        // Simulate some async work
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(RandomInstance.Next(10, 50)));

        // Verify context hasn't changed
        await Assert.That(TestContext.Current).IsSameReferenceAs(context);
        await Assert.That(TestContext.Current!.StateBag.Items["TestLocalId"]).IsEqualTo(testId);

        // Verify AsyncLocal is preserved
        await Assert.That(TestLocalValue.Value).IsEqualTo(testId);

        // Store for cross-test verification
        CapturedContexts[testId!] = context;

        // More async work
        await Task.Yield();

        // Final verification
        await Assert.That(TestContext.Current).IsSameReferenceAs(context);
    }

    [Test]
    [Repeat(5)]
    public async Task TestContext_Should_Be_Isolated_In_Parallel_Test2()
    {
        var context = TestContext.Current;
        await Assert.That(context).IsNotNull();

        var testId = context!.StateBag.Items["TestLocalId"] as string;
        await Assert.That(testId).IsNotNull();

        // Different delay pattern
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(RandomInstance.Next(5, 30)));

        // Verify isolation
        await Assert.That(TestContext.Current).IsSameReferenceAs(context);
        await Assert.That(TestContext.Current!.StateBag.Items["TestLocalId"]).IsEqualTo(testId);
        await Assert.That(TestLocalValue.Value).IsEqualTo(testId);

        CapturedContexts[testId!] = context;

        await Task.Yield();
        await Assert.That(TestContext.Current).IsSameReferenceAs(context);
    }

    [Test]
    [Repeat(5)]
    public async Task TestContext_Should_Be_Isolated_In_Sync_Test()
    {
        var context = TestContext.Current;
        await Assert.That(context).IsNotNull();

        var testId = context!.StateBag.Items["TestLocalId"] as string;
        await Assert.That(testId).IsNotNull();

        // Simulate work
        Thread.Sleep(RandomInstance.Next(10, 50));

        // Verify context remains the same
        await Assert.That(TestContext.Current).IsSameReferenceAs(context);
        await Assert.That(TestContext.Current!.StateBag.Items["TestLocalId"]).IsEqualTo(testId);
        await Assert.That(TestLocalValue.Value).IsEqualTo(testId);

        CapturedContexts[testId!] = context;
    }

    [Test]
    [DependsOn(nameof(TestContext_Should_Be_Isolated_In_Parallel_Test1))]
    [DependsOn(nameof(TestContext_Should_Be_Isolated_In_Parallel_Test2))]
    [DependsOn(nameof(TestContext_Should_Be_Isolated_In_Sync_Test))]
    public async Task Verify_All_Contexts_Were_Unique()
    {
        // Wait a bit to ensure all tests have completed storing their contexts
        var timeProvider = TestContext.Current!.TimeProvider;
            await timeProvider.Delay(TimeSpan.FromMilliseconds(100));

        // Each test execution should have had a unique context
        var allContexts = CapturedContexts.Values.Where(c => c != null).ToList();

        // Verify we captured contexts
        await Assert.That(allContexts).HasCount().GreaterThanOrEqualTo(15); // 3 tests * 5 repeats

        // Verify all contexts are unique instances
        var uniqueContexts = allContexts.Distinct().ToList();
        await Assert.That(uniqueContexts).HasCount().EqualTo(allContexts.Count);

        // Verify each test had its own TestLocalId
        var allTestIds = CapturedContexts.Keys.ToList();
        var uniqueTestIds = allTestIds.Distinct().ToList();
        await Assert.That(uniqueTestIds).HasCount().EqualTo(allTestIds.Count);
    }
}

/// <summary>
/// Tests for context isolation with nested async operations
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class TestContextNestedAsyncIsolationTests
{
    private static readonly ConcurrentBag<(string TestName, TestContext? Context, int ThreadId)> ObservedContexts = new();

    [Test]
    [Repeat(3)]
    public async Task Context_Should_Be_Preserved_Through_Nested_Async_Calls_Test1()
    {
        var initialContext = TestContext.Current;
        await Assert.That(initialContext).IsNotNull();

        var testName = initialContext!.Metadata.TestDetails.TestName;
        ObservedContexts.Add((testName, initialContext, Thread.CurrentThread.ManagedThreadId));

        await NestedAsyncMethod1(initialContext);

        // Context should still be the same after async operations
        await Assert.That(TestContext.Current).IsSameReferenceAs(initialContext);
    }

    [Test]
    [Repeat(3)]
    public async Task Context_Should_Be_Preserved_Through_Nested_Async_Calls_Test2()
    {
        var initialContext = TestContext.Current;
        await Assert.That(initialContext).IsNotNull();

        var testName = initialContext!.Metadata.TestDetails.TestName;
        ObservedContexts.Add((testName, initialContext, Thread.CurrentThread.ManagedThreadId));

        await NestedAsyncMethod2(initialContext);

        await Assert.That(TestContext.Current).IsSameReferenceAs(initialContext);
    }

    private async Task NestedAsyncMethod1(TestContext expectedContext)
    {
        var timeProvider = TestContext.Current!.TimeProvider;
            await timeProvider.Delay(TimeSpan.FromMilliseconds(10));
        await Assert.That(TestContext.Current).IsSameReferenceAs(expectedContext);

        await Task.Run(async () =>
        {
            // Even in Task.Run, context should be preserved
            await Assert.That(TestContext.Current).IsSameReferenceAs(expectedContext);
            var timeProvider = TestContext.Current!.TimeProvider;
            await timeProvider.Delay(TimeSpan.FromMilliseconds(5));
            await Assert.That(TestContext.Current).IsSameReferenceAs(expectedContext);
        });

        await Assert.That(TestContext.Current).IsSameReferenceAs(expectedContext);
    }

    private async Task NestedAsyncMethod2(TestContext expectedContext)
    {
        // Different async pattern
        await Task.Yield();
        await Assert.That(TestContext.Current).IsSameReferenceAs(expectedContext);

        var tasks = Enumerable.Range(0, 3).Select(async i =>
        {
            var timeProvider = TestContext.Current!.TimeProvider;
                await timeProvider.Delay(TimeSpan.FromMilliseconds(i * 5));
            await Assert.That(TestContext.Current).IsSameReferenceAs(expectedContext);
        });

        await Task.WhenAll(tasks);
        await Assert.That(TestContext.Current).IsSameReferenceAs(expectedContext);
    }
}

/// <summary>
/// Tests for potential race conditions in TestContext.Current
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class TestContextRaceConditionTests
{
    private static readonly object LockObject = new();
    private static volatile int ConcurrentTestCount = 0;
    private static readonly ConcurrentBag<string> DetectedContextMismatches = new();

    [Test]
    [Repeat(10)] // More repeats to catch race conditions
    public async Task Concurrent_Tests_Should_Not_Share_Context()
    {
        var myContext = TestContext.Current;
        await Assert.That(myContext).IsNotNull();

        var myTestName = myContext!.Metadata.TestDetails.TestName;
        var myTestId = Guid.NewGuid().ToString();
        myContext.StateBag.Items["UniqueTestId"] = myTestId;

        Interlocked.Increment(ref ConcurrentTestCount);

        // Try to create race conditions
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    await Task.Yield();

                    // Check if context has changed unexpectedly
                    var currentContext = TestContext.Current;
                    if (currentContext != myContext)
                    {
                        DetectedContextMismatches.Add($"Context mismatch in {myTestName}: Expected {myTestId}, Current context: {currentContext?.StateBag.Items.GetValueOrDefault("UniqueTestId")}");
                    }

                    if (currentContext?.StateBag.Items.GetValueOrDefault("UniqueTestId") as string != myTestId)
                    {
                        DetectedContextMismatches.Add($"TestId mismatch in {myTestName}: Expected {myTestId}, Got {currentContext?.StateBag.Items.GetValueOrDefault("UniqueTestId")}");
                    }

                    var timeProvider = TestContext.Current!.TimeProvider;
            await timeProvider.Delay(TimeSpan.FromMilliseconds(1));
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Final verification
        await Assert.That(TestContext.Current).IsSameReferenceAs(myContext);
        await Assert.That(TestContext.Current!.StateBag.Items["UniqueTestId"]).IsEqualTo(myTestId);
    }

    [Test]
    [DependsOn(nameof(Concurrent_Tests_Should_Not_Share_Context))]
    public async Task Verify_No_Context_Mismatches_Detected()
    {
        // This test runs after all concurrent tests
        if (DetectedContextMismatches.Any())
        {
            var mismatches = string.Join("\n", DetectedContextMismatches.Distinct());
            Assert.Fail($"Context mismatches detected:\n{mismatches}");
        }

        // Verify we actually ran concurrent tests
        await Assert.That(ConcurrentTestCount).IsGreaterThanOrEqualTo(10);
    }
}

/// <summary>
/// Tests for TestContext with different hooks
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class TestContextHookIsolationTests
{
    private static TestContext? BeforeTestContext;
    private static TestContext? TestMethodContext;
    private static TestContext? AfterTestContext;

    [Before(Test)]
    public async Task BeforeTest()
    {
        BeforeTestContext = TestContext.Current;
        await Assert.That(BeforeTestContext).IsNotNull();
    }

    [Test]
    public async Task TestContext_Should_Be_Same_In_Hooks_And_Test()
    {
        TestMethodContext = TestContext.Current;
        await Assert.That(TestMethodContext).IsNotNull();

        // Context in Before hook should be the same as in test
        await Assert.That(TestMethodContext).IsSameReferenceAs(BeforeTestContext);
    }

    [After(Test)]
    public async Task AfterTest()
    {
        AfterTestContext = TestContext.Current;
        await Assert.That(AfterTestContext).IsNotNull();

        // Context should be consistent across all hooks and test
        await Assert.That(AfterTestContext).IsSameReferenceAs(TestMethodContext);
        await Assert.That(AfterTestContext).IsSameReferenceAs(BeforeTestContext);
    }
}
