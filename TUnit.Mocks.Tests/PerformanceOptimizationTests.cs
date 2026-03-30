using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Tests targeting the internal performance optimizations:
/// - Flat array-based setup storage (replacing Dictionary snapshots)
/// - Per-member call indexing and counters (replacing ConcurrentQueue linear scans)
/// - Fast-path verification when no argument matchers are present
/// - Lock-based call recording thread safety
/// </summary>
public class PerformanceOptimizationTests
{
    // ========================================================================
    // Per-member call indexing and GetCallCountFor
    // ========================================================================

    [Test]
    public async Task Invocations_Track_Per_Member_Counts_Correctly()
    {
        // Arrange — ICalculator has 3 members: Add (0), GetName (1), Log (2)
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        mock.GetName().Returns("test");
        ICalculator calc = mock.Object;

        // Act — call different members different numbers of times
        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(5, 6);
        calc.GetName();
        calc.Log("msg1");
        calc.Log("msg2");

        // Assert — total invocations correct
        await Assert.That(mock.Invocations).HasCount().EqualTo(6);

        // Verify per-member counts via WasCalled
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(3));
        mock.GetName().WasCalled(Times.Once);
        mock.Log(Any()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Invocations_Per_Member_Independent_After_Reset()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        ICalculator calc = mock.Object;

        // Act — call, reset, call again
        calc.Add(1, 2);
        calc.Add(3, 4);
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(2));

        mock.Reset();
        mock.Add(Any(), Any()).Returns(99);

        calc.Add(5, 6);

        // Assert — only one call after reset
        mock.Add(Any(), Any()).WasCalled(Times.Once);
        await Assert.That(mock.Invocations).HasCount().EqualTo(1);
    }

    [Test]
    public async Task GetCallsFor_Returns_Only_Matching_Member_Calls()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        mock.GetName().Returns("test");
        ICalculator calc = mock.Object;

        // Act
        calc.Add(1, 2);
        calc.GetName();
        calc.Add(3, 4);
        calc.GetName();
        calc.GetName();

        // Assert — verify correct counts per member
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(2));
        mock.GetName().WasCalled(Times.Exactly(3));
        await Assert.That(mock.Invocations).HasCount().EqualTo(5);
    }

    // ========================================================================
    // Setup storage: flat array with multiple members
    // ========================================================================

    [Test]
    public async Task Setups_For_Multiple_Members_Work_Independently()
    {
        // Arrange — setup all 3 members of ICalculator
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(3);
        mock.Add(10, 20).Returns(30);
        mock.GetName().Returns("calculator");

        ICalculator calc = mock.Object;

        // Act & Assert — each member has independent setups
        await Assert.That(calc.Add(1, 2)).IsEqualTo(3);
        await Assert.That(calc.Add(10, 20)).IsEqualTo(30);
        await Assert.That(calc.GetName()).IsEqualTo("calculator");
    }

    [Test]
    public async Task Many_Setups_On_Same_Member_Last_Wins()
    {
        // Arrange — multiple setups with same args, last should win
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(10);
        mock.Add(1, 2).Returns(20);
        mock.Add(1, 2).Returns(30);

        ICalculator calc = mock.Object;

        // Assert — last setup wins
        await Assert.That(calc.Add(1, 2)).IsEqualTo(30);
    }

    [Test]
    public async Task Setup_Storage_Handles_Interface_With_Many_Members()
    {
        // Arrange — IUserRepository has 7 methods, testing that the flat array
        // handles larger interfaces correctly
        var mock = Mock.Of<IUserRepository>();
        var user = new UserDto { Id = 1, Name = "Alice" };

        mock.GetByIdAsync(1).Returns(user);
        mock.ExistsAsync(1).Returns(true);
        mock.ExistsAsync(2).Returns(false);
        mock.GetAllAsync().Returns((IReadOnlyList<UserDto>)new List<UserDto> { user });

        var repo = mock.Object;

        // Act & Assert
        await Assert.That(await repo.GetByIdAsync(1)).IsEqualTo(user);
        await Assert.That(await repo.ExistsAsync(1)).IsTrue();
        await Assert.That(await repo.ExistsAsync(2)).IsFalse();
        var all = await repo.GetAllAsync();
        await Assert.That(all).HasCount().EqualTo(1);
    }

    // ========================================================================
    // Fast-path verification (no argument matchers)
    // ========================================================================

    [Test]
    public async Task Verification_Zero_Param_Method_Uses_Fast_Path()
    {
        // GetName() has zero parameters, so _matchers.Length == 0 → fast path (per-member counter)
        var mock = Mock.Of<ICalculator>();
        mock.GetName().Returns("test");
        ICalculator calc = mock.Object;

        calc.GetName();
        calc.GetName();
        calc.GetName();

        mock.GetName().WasCalled(Times.Exactly(3));
        mock.GetName().WasCalled(Times.AtLeast(2));
        mock.GetName().WasCalled(Times.AtMost(5));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verification_With_Any_Matchers_Uses_Matcher_Path()
    {
        // Any() creates argument matchers, so _matchers.Length > 0 → matcher path
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(5, 6);

        mock.Add(Any(), Any()).WasCalled(Times.Exactly(3));
        mock.Add(Any(), Any()).WasCalled(Times.AtLeast(2));
        mock.Add(Any(), Any()).WasCalled(Times.AtMost(5));
        mock.GetName().WasNeverCalled();
        mock.Log(Any()).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verification_With_Exact_Args_Uses_Matcher_Path()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.Add(3, 4);

        // Exact arg matching only counts matching calls
        mock.Add(1, 2).WasCalled(Times.Exactly(2));
        mock.Add(3, 4).WasCalled(Times.Once);
        mock.Add(5, 6).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verification_WasNeverCalled_Zero_Param_Method()
    {
        var mock = Mock.Of<ICalculator>();

        // Zero-param method → fast path with per-member counter = 0
        mock.GetName().WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verification_WasNeverCalled_With_Matchers()
    {
        var mock = Mock.Of<ICalculator>();

        // Any() creates matchers → matcher path, but still zero calls
        mock.Add(Any(), Any()).WasNeverCalled();
        mock.Log(Any()).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verification_Failure_With_No_Matchers_Shows_Correct_Message()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert — expecting 3 calls but only 1 was made
        var ex = Assert.Throws<MockVerificationException>(
            () => mock.Add(Any(), Any()).WasCalled(Times.Exactly(3)));
        await Assert.That(ex).IsNotNull();
    }

    // ========================================================================
    // Thread safety with new lock-based call recording
    // ========================================================================

    [Test]
    public async Task Concurrent_Calls_To_Multiple_Members_All_Recorded()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        mock.GetName().Returns("test");
        ICalculator calc = mock.Object;

        // Act — 50 concurrent Add calls + 50 concurrent GetName calls
        var addTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() => { calc.Add(i, i); return 0; }));
        var nameTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() => { calc.GetName(); return 0; }));
        await Task.WhenAll(addTasks.Concat(nameTasks));

        // Assert — all 100 calls recorded
        await Assert.That(mock.Invocations).HasCount().EqualTo(100);
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(50));
        mock.GetName().WasCalled(Times.Exactly(50));
    }

    [Test]
    public async Task Concurrent_Setup_Then_Calls_Across_Members()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — setup and call concurrently across members
        var setupAndCallTasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
        {
            mock.Add(Any(), Any()).Returns(i);
            mock.Object.Add(i, i);
        }));
        await Task.WhenAll(setupAndCallTasks);

        // Assert — all calls should be recorded
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(20));
    }

    [Test]
    public async Task Concurrent_Verification_With_Per_Member_Counters()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        ICalculator calc = mock.Object;

        // Make 100 calls first
        for (int i = 0; i < 100; i++)
        {
            calc.Add(i, i);
        }

        // Act — 20 concurrent verifications should all succeed
        var verifyTasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            mock.Add(Any(), Any()).WasCalled(Times.Exactly(100));
        }));
        await Task.WhenAll(verifyTasks);
        await Assert.That(true).IsTrue();
    }

    // ========================================================================
    // VerifyAll and VerifyNoOtherCalls with new data structures
    // ========================================================================

    [Test]
    public async Task VerifyAll_Works_With_Flat_Array_Setup_Storage()
    {
        // Arrange — setups across multiple members
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(3);
        mock.GetName().Returns("test");

        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.GetName();

        // Assert — VerifyAll should iterate all setups in flat array
        mock.VerifyAll();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyAll_Fails_When_Setup_Not_Invoked_With_Flat_Array()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(3);
        mock.GetName().Returns("test");

        ICalculator calc = mock.Object;
        calc.Add(1, 2); // Only call Add, not GetName

        // Assert — should fail because GetName setup was not invoked
        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyAll());
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Works_With_Per_Member_Index()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.Add(3, 4);

        // Verify all calls
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(2));

        // Assert — no unverified calls
        mock.VerifyNoOtherCalls();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Fails_With_Unverified_Member()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        mock.GetName().Returns("test");
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.GetName();

        // Only verify Add, not GetName
        mock.Add(Any(), Any()).WasCalled(Times.Once);

        // Assert — GetName call is unverified
        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyNoOtherCalls());
        await Assert.That(ex).IsNotNull();
    }

    // ========================================================================
    // Reset clears new data structures completely
    // ========================================================================

    [Test]
    public async Task Reset_Clears_Per_Member_Call_Index_And_Counters()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(5, 6);
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(3));

        // Act
        mock.Reset();

        // Assert — all counters reset
        mock.Add(Any(), Any()).WasNeverCalled();
        mock.GetName().WasNeverCalled();
        mock.Log(Any()).WasNeverCalled();
        await Assert.That(mock.Invocations).HasCount().EqualTo(0);
    }

    [Test]
    public async Task Reset_Clears_Flat_Array_Setups_And_Allows_Reconfiguration()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(100);
        mock.GetName().Returns("first");
        ICalculator calc = mock.Object;

        await Assert.That(calc.Add(1, 2)).IsEqualTo(100);
        await Assert.That(calc.GetName()).IsEqualTo("first");

        // Act
        mock.Reset();
        mock.Add(1, 2).Returns(200);
        mock.GetName().Returns("second");

        // Assert — new setups active
        await Assert.That(calc.Add(1, 2)).IsEqualTo(200);
        await Assert.That(calc.GetName()).IsEqualTo("second");
    }

    [Test]
    public async Task Multiple_Reset_Cycles_Work_Correctly()
    {
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        for (int cycle = 0; cycle < 5; cycle++)
        {
            mock.Add(Any(), Any()).Returns(cycle);
            calc.Add(1, 2);
            await Assert.That(calc.Add(1, 2)).IsEqualTo(cycle);
            mock.Add(Any(), Any()).WasCalled(Times.Exactly(2));
            mock.Reset();
        }

        // Final state: clean
        mock.Add(Any(), Any()).WasNeverCalled();
        await Assert.That(mock.Invocations).HasCount().EqualTo(0);
    }

    // ========================================================================
    // Edge cases for array growth/sizing
    // ========================================================================

    [Test]
    public async Task Verification_Before_Any_Setup_Or_Call()
    {
        // Arrange — fresh mock, no setups, no calls
        var mock = Mock.Of<ICalculator>();

        // Assert — should not throw, all members have zero calls
        mock.Add(Any(), Any()).WasNeverCalled();
        mock.GetName().WasNeverCalled();
        mock.Log(Any()).WasNeverCalled();
        await Assert.That(mock.Invocations).HasCount().EqualTo(0);
    }

    [Test]
    public async Task Call_Without_Setup_Still_Recorded_In_Per_Member_Index()
    {
        // Arrange — no setup, loose mode
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — call without any setup
        calc.Add(1, 2);
        calc.Log("test");
        calc.GetName();

        // Assert — all calls recorded even without setups
        await Assert.That(mock.Invocations).HasCount().EqualTo(3);
        mock.Add(Any(), Any()).WasCalled(Times.Once);
        mock.Log(Any()).WasCalled(Times.Once);
        mock.GetName().WasCalled(Times.Once);
    }
}
