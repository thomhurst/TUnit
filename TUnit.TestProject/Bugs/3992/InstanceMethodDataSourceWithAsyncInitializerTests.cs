using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3992;

/// <summary>
/// Regression test for issue #3992: IAsyncInitializer should not run during test discovery
/// when using InstanceMethodDataSource with ClassDataSource.
///
/// This test replicates the user's scenario where:
/// 1. A ClassDataSource fixture implements IAsyncInitializer (e.g., starts Docker containers)
/// 2. An InstanceMethodDataSource accesses data from that fixture
/// 3. The fixture should NOT be initialized during discovery - only during execution
///
/// The bug caused Docker containers to start during test discovery (e.g., in IDE or --list-tests),
/// which was unexpected and resource-intensive.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class InstanceMethodDataSourceWithAsyncInitializerTests
{
    private static int _initializationCount;
    private static int _testExecutionCount;
    private static readonly ConcurrentBag<Guid> _observedInstanceIds = [];

    /// <summary>
    /// Simulates a fixture like ClientServiceFixture that starts Docker containers.
    /// Implements IAsyncInitializer (NOT IAsyncDiscoveryInitializer) because the user
    /// does not want initialization during discovery.
    /// </summary>
    public class SimulatedContainerFixture : IAsyncInitializer
    {
        private readonly List<string> _testCases = [];

        /// <summary>
        /// Unique identifier for this instance to verify sharing behavior.
        /// </summary>
        public Guid InstanceId { get; } = Guid.NewGuid();

        public bool IsInitialized { get; private set; }
        public IReadOnlyList<string> TestCases => _testCases;

        public Task InitializeAsync()
        {
            Interlocked.Increment(ref _initializationCount);
            Console.WriteLine($"[SimulatedContainerFixture] InitializeAsync called on instance {InstanceId} (count: {_initializationCount})");

            // Simulate container startup that populates test data
            _testCases.AddRange(["TestCase1", "TestCase2", "TestCase3"]);
            IsInitialized = true;

            return Task.CompletedTask;
        }
    }

    [ClassDataSource<SimulatedContainerFixture>(Shared = SharedType.PerClass)]
    public required SimulatedContainerFixture Fixture { get; init; }

    /// <summary>
    /// This property is accessed by InstanceMethodDataSource during discovery.
    /// With the bug, accessing this would trigger InitializeAsync() during discovery.
    /// After the fix, InitializeAsync() should only be called during test execution.
    /// </summary>
    public IEnumerable<string> TestExecutions => Fixture.TestCases;

    [Test]
    [InstanceMethodDataSource(nameof(TestExecutions))]
    public async Task Test_WithInstanceMethodDataSource_DoesNotInitializeDuringDiscovery(string testCase)
    {
        Interlocked.Increment(ref _testExecutionCount);

        // Track this instance to verify sharing
        _observedInstanceIds.Add(Fixture.InstanceId);

        // The fixture should be initialized by the time the test runs
        await Assert.That(Fixture.IsInitialized)
            .IsTrue()
            .Because("the fixture should be initialized before test execution");

        await Assert.That(testCase)
            .IsNotNullOrEmpty()
            .Because("the test case data should be available");

        Console.WriteLine($"[Test] Executed with testCase='{testCase}', instanceId={Fixture.InstanceId}, " +
                          $"initCount={_initializationCount}, execCount={_testExecutionCount}");
    }

    [After(Class)]
    public static async Task VerifyInitializationAndSharing()
    {
        // With SharedType.PerClass, the fixture should be initialized exactly ONCE
        // during test execution, NOT during discovery.
        //
        // Before the fix: _initializationCount would be 2+ (discovery + execution)
        // After the fix: _initializationCount should be exactly 1 (execution only)

        Console.WriteLine($"[After(Class)] Final counts - init: {_initializationCount}, exec: {_testExecutionCount}");
        Console.WriteLine($"[After(Class)] Unique instance IDs observed: {_observedInstanceIds.Distinct().Count()}");

        await Assert.That(_initializationCount)
            .IsEqualTo(1)
            .Because("IAsyncInitializer should only be called once during execution, not during discovery");

        await Assert.That(_testExecutionCount)
            .IsEqualTo(3)
            .Because("there should be 3 test executions (one per test case)");

        // Verify that all tests used the SAME fixture instance (SharedType.PerClass)
        var uniqueInstanceIds = _observedInstanceIds.Distinct().ToList();
        await Assert.That(uniqueInstanceIds)
            .HasCount().EqualTo(1)
            .Because("with SharedType.PerClass, all tests should share the same fixture instance");

        // Reset for next run
        _initializationCount = 0;
        _testExecutionCount = 0;
        _observedInstanceIds.Clear();
    }
}
