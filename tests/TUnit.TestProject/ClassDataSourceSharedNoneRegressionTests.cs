using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Regression tests for GitHub Issue #3855
/// Verifies that ClassDataSource with SharedType.None constructs and initializes objects only once per test.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ClassDataSourceSharedNoneRegressionTests
{
    // Test helper class that tracks construction
    public class ConstructionCounterClass
    {
        private static int _instanceCounter = 0;
        public int InstanceNumber { get; }

        public ConstructionCounterClass()
        {
            InstanceNumber = Interlocked.Increment(ref _instanceCounter);
        }

        public int Value { get; set; } = 42;
    }

    // Test helper class that tracks initialization
    public class AsyncInitializerCounterClass : IAsyncInitializer
    {
        private static int _initCounter = 0;
        public int InitNumber { get; private set; } = -1;

        public Task InitializeAsync()
        {
            InitNumber = Interlocked.Increment(ref _initCounter);
            return Task.CompletedTask;
        }

        public int Value { get; set; } = 99;
    }

    /// <summary>
    /// Test that SharedType.None constructs the class data source exactly once per test.
    /// Regression test for issue #3855 where it was being constructed twice.
    /// Before the fix, InstanceNumber would be 2 (constructed twice).
    /// After the fix, InstanceNumber should be 1 (constructed once).
    /// </summary>
    [Test]
    [ClassDataSource<ConstructionCounterClass>(Shared = SharedType.None)]
    public async Task SharedTypeNone_ConstructsOnlyOnce(ConstructionCounterClass instance)
    {
        // Instance should be constructed exactly once, so InstanceNumber should be 1
        // (not 2, which would indicate double construction)
        await Assert.That(instance).IsNotNull();
        await Assert.That(instance.InstanceNumber).IsEqualTo(1);
        await Assert.That(instance.Value).IsEqualTo(42);
    }

    /// <summary>
    /// Test that SharedType.None with IAsyncInitializer initializes exactly once per test.
    /// Regression test for issue #3855 where InitializeAsync was being called twice.
    /// Before the fix, InitNumber would be 2 (initialized twice).
    /// After the fix, InitNumber should be 1 (initialized once).
    /// </summary>
    [Test]
    [ClassDataSource<AsyncInitializerCounterClass>(Shared = SharedType.None)]
    public async Task SharedTypeNone_WithAsyncInitializer_InitializesOnlyOnce(AsyncInitializerCounterClass instance)
    {
        // Instance should be initialized exactly once, so InitNumber should be 1
        // (not 2, which would indicate double initialization)
        await Assert.That(instance).IsNotNull();
        await Assert.That(instance.InitNumber).IsEqualTo(1);
        await Assert.That(instance.Value).IsEqualTo(99);
    }
}

/// <summary>
/// Regression tests for GitHub Issue #3855 - Verify no instance leakage across tests.
/// Tests that multiple test methods in the same class each receive their own unique instance
/// with SharedType.None (no sharing or leakage across tests).
/// The order in which instances are constructed relative to test execution is not fixed,
/// so these tests assert distinct instance IDs rather than specific values (#7250).
/// </summary>
public class ClassDataSourceSharedNoneNoLeakageTests
{
    // Test helper class that tracks instance IDs and mutation
    public class UniqueInstanceClass
    {
        private static int _nextId = 0;
        public int InstanceId { get; }
        public int MutationValue { get; set; }

        public UniqueInstanceClass()
        {
            InstanceId = Interlocked.Increment(ref _nextId);
            MutationValue = 0; // Initial value
        }
    }

    // Instance ID -> name of the test that received it
    private static readonly ConcurrentDictionary<int, string> ReceivedInstanceIds = new();

    private static async Task AssertUniqueUnmutatedInstance(UniqueInstanceClass instance, string testName)
    {
        // Another test receiving the same instance would mean the instance was shared
        var owner = ReceivedInstanceIds.GetOrAdd(instance.InstanceId, testName);
        await Assert.That(owner).IsEqualTo(testName);

        // MutationValue should be 0 (not affected by another test's mutation)
        await Assert.That(instance.MutationValue).IsEqualTo(0);
    }

    [Test]
    [ClassDataSource<UniqueInstanceClass>(Shared = SharedType.None)]
    public async Task Test1_GetsUniqueInstance(UniqueInstanceClass instance)
    {
        await AssertUniqueUnmutatedInstance(instance, nameof(Test1_GetsUniqueInstance));
        instance.MutationValue = 100;
    }

    [Test]
    [ClassDataSource<UniqueInstanceClass>(Shared = SharedType.None)]
    public async Task Test2_GetsUniqueInstance(UniqueInstanceClass instance)
    {
        await AssertUniqueUnmutatedInstance(instance, nameof(Test2_GetsUniqueInstance));
        instance.MutationValue = 200;
    }

    [Test]
    [ClassDataSource<UniqueInstanceClass>(Shared = SharedType.None)]
    public async Task Test3_GetsUniqueInstance(UniqueInstanceClass instance)
    {
        await AssertUniqueUnmutatedInstance(instance, nameof(Test3_GetsUniqueInstance));
        instance.MutationValue = 300;
    }
}
