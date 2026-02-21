using TUnit.Mock;
using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Tests;

// ─── Interfaces for Edge Case Tests ───────────────────────────────────────────

#region 1. Method Overloads

public interface IOverloadedService
{
    string Format(int value);
    string Format(string value);
    string Format(int value, string prefix);
    string Format(double value, int decimals);
    void Process(string data);
    void Process(string data, bool validate);
}

#endregion

#region 2. Large Parameter Count Methods

public interface IComplexOperations
{
    string BuildQuery(string table, string[] columns, string? whereClause, int? limit, int? offset, string? orderBy, bool ascending);
    Task<bool> ProcessPaymentAsync(string merchantId, decimal amount, string currency, string cardToken, string? referenceId, CancellationToken cancellationToken = default);
}

#endregion

#region 3. Interface Inheritance (Multiple Levels)

public interface IEntity
{
    int Id { get; }
    DateTime CreatedAt { get; }
}

public interface IAuditable : IEntity
{
    string CreatedBy { get; }
    DateTime? ModifiedAt { get; }
    string? ModifiedBy { get; }
}

public interface ISoftDeletable : IAuditable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    string? DeletedBy { get; }
}

public interface IAuditRepository
{
    Task<ISoftDeletable> GetByIdAsync(int id);
    Task<IReadOnlyList<ISoftDeletable>> GetActiveAsync();
}

#endregion

#region 4. Exception Scenarios

public interface IExternalApi
{
    Task<string> CallRemoteServiceAsync(string endpoint);
    string GetConfig(string key);
}

#endregion

#region 5. Complex Argument Matching

public interface IValidator
{
    bool Validate(string input);
    Task<ValidationResult> ValidateAsync(object payload);
    int Score(string text, int weight);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion

#region 6. Concurrent Access

public interface IConcurrentService
{
    Task<int> IncrementAsync(string key);
    int GetValue(string key);
}

#endregion

#region 7. Enum Parameters

public enum Priority { Low, Medium, High, Critical }
public enum Status { Pending, Active, Completed, Failed }

public interface ITaskManager
{
    Task<List<TaskItem>> GetByPriorityAsync(Priority priority);
    void UpdateStatus(int taskId, Status status);
    Task<int> CountByStatusAsync(Status status);
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public Priority Priority { get; set; }
    public Status Status { get; set; }
}

#endregion

// ─── Test Classes ─────────────────────────────────────────────────────────────

/// <summary>
/// Edge case tests for method overload resolution in mock setup and verification.
/// </summary>
public class OverloadEdgeCaseTests
{
    [Test]
    public async Task Overload_Int_Vs_String_Different_Setups()
    {
        // Arrange
        var mock = Mock.Of<IOverloadedService>();
        mock.Setup.Format(42).Returns("int:42");
        mock.Setup.Format("hello").Returns("str:hello");

        // Act
        IOverloadedService svc = mock.Object;
        var intResult = svc.Format(42);
        var strResult = svc.Format("hello");

        // Assert — each overload returns its own configured value
        await Assert.That(intResult).IsEqualTo("int:42");
        await Assert.That(strResult).IsEqualTo("str:hello");
    }

    [Test]
    public async Task Overload_Two_Params_Different_Signatures()
    {
        // Arrange
        var mock = Mock.Of<IOverloadedService>();
        mock.Setup.Format(10, "PREFIX").Returns("int-string:PREFIX10");
        mock.Setup.Format(3.14, 2).Returns("double-int:3.14:2");

        // Act
        IOverloadedService svc = mock.Object;
        var intStrResult = svc.Format(10, "PREFIX");
        var dblIntResult = svc.Format(3.14, 2);

        // Assert — different overloads resolved correctly
        await Assert.That(intStrResult).IsEqualTo("int-string:PREFIX10");
        await Assert.That(dblIntResult).IsEqualTo("double-int:3.14:2");
    }

    [Test]
    public async Task Overload_Void_With_Optional_Bool()
    {
        // Arrange
        var singleArgCalled = false;
        var twoArgCalled = false;
        var mock = Mock.Of<IOverloadedService>();
        mock.Setup.Process(Arg.Any<string>())
            .Callback((Action)(() => singleArgCalled = true));
        mock.Setup.Process(Arg.Any<string>(), Arg.Any<bool>())
            .Callback((Action)(() => twoArgCalled = true));

        // Act
        IOverloadedService svc = mock.Object;
        svc.Process("data");
        svc.Process("data", true);

        // Assert — correct overload callback was invoked
        await Assert.That(singleArgCalled).IsTrue();
        await Assert.That(twoArgCalled).IsTrue();
    }

    [Test]
    public async Task Overload_Verify_Specific_Overload()
    {
        // Arrange
        var mock = Mock.Of<IOverloadedService>();
        mock.Setup.Format(Arg.Any<int>()).Returns("int");
        mock.Setup.Format(Arg.Any<string>()).Returns("string");

        // Act
        IOverloadedService svc = mock.Object;
        svc.Format(1);
        svc.Format(2);
        svc.Format("abc");

        // Assert — verify only the int overload was called twice
        mock.Verify.Format(Arg.Any<int>()).WasCalled(Times.Exactly(2));
        mock.Verify.Format(Arg.Any<string>()).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }
}

/// <summary>
/// Edge case tests for methods with large parameter counts and mixed matchers.
/// </summary>
public class LargeParameterCountTests
{
    [Test]
    public async Task Seven_Parameter_Method_With_Mixed_Matchers()
    {
        // Arrange
        var mock = Mock.Of<IComplexOperations>();
        mock.Setup.BuildQuery(
            "users",
            Arg.Any<string[]>(),
            Arg.Is<string?>(w => w != null && w.Contains("active")),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            "name",
            true
        ).Returns("SELECT * FROM users WHERE active ORDER BY name ASC");

        // Act
        IComplexOperations ops = mock.Object;
        var result = ops.BuildQuery(
            "users",
            ["id", "name", "email"],
            "status = 'active'",
            100,
            0,
            "name",
            true
        );

        // Assert
        await Assert.That(result).IsEqualTo("SELECT * FROM users WHERE active ORDER BY name ASC");

        // Non-matching where clause returns default
        var defaultResult = ops.BuildQuery(
            "users",
            ["id"],
            "deleted = true",
            10,
            0,
            "name",
            true
        );
        await Assert.That(defaultResult).IsNotEqualTo("SELECT * FROM users WHERE active ORDER BY name ASC");
    }

    [Test]
    public async Task Payment_Method_With_CancellationToken_And_Nullable()
    {
        // Arrange
        var mock = Mock.Of<IComplexOperations>();
        mock.Setup.ProcessPaymentAsync(
            "merchant-123",
            Arg.Is<decimal>(a => a > 0),
            "USD",
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>()
        ).Returns(true);

        // Act
        IComplexOperations ops = mock.Object;
        using var cts = new CancellationTokenSource();
        var result = await ops.ProcessPaymentAsync(
            "merchant-123",
            99.99m,
            "USD",
            "tok_abc123",
            null,
            cts.Token
        );

        // Assert
        await Assert.That(result).IsTrue();

        // Verify the call was made
        mock.Verify.ProcessPaymentAsync(
            "merchant-123",
            Arg.Any<decimal>(),
            "USD",
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>()
        ).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }
}

/// <summary>
/// Edge case tests for deep interface inheritance — verifying all inherited members are accessible.
/// </summary>
public class InterfaceInheritanceTests
{
    [Test]
    public async Task Deep_Interface_Inheritance_All_Properties_Accessible()
    {
        // Arrange
        var mock = Mock.Of<ISoftDeletable>();
        var now = DateTime.UtcNow;

        // Setup properties from all three levels of inheritance
        // IEntity
        mock.Setup.Id_Get().Returns(42);
        mock.Setup.CreatedAt_Get().Returns(now);
        // IAuditable
        mock.Setup.CreatedBy_Get().Returns("admin");
        mock.Setup.ModifiedAt_Get().Returns(now.AddHours(1));
        mock.Setup.ModifiedBy_Get().Returns("editor");
        // ISoftDeletable
        mock.Setup.IsDeleted_Get().Returns(false);
        mock.Setup.DeletedAt_Get().Returns((DateTime?)null);
        mock.Setup.DeletedBy_Get().Returns((string?)null);

        // Act
        ISoftDeletable entity = mock.Object;

        // Assert — all properties from all levels accessible
        await Assert.That(entity.Id).IsEqualTo(42);
        await Assert.That(entity.CreatedAt).IsEqualTo(now);
        await Assert.That(entity.CreatedBy).IsEqualTo("admin");
        await Assert.That(entity.ModifiedAt).IsEqualTo(now.AddHours(1));
        await Assert.That(entity.ModifiedBy).IsEqualTo("editor");
        await Assert.That(entity.IsDeleted).IsFalse();
        await Assert.That(entity.DeletedAt).IsNull();
        await Assert.That(entity.DeletedBy).IsNull();
    }

    [Test]
    public async Task Repository_Returning_Inherited_Interface()
    {
        // Arrange — create a mock ISoftDeletable to use as the return value
        var entityMock = Mock.Of<ISoftDeletable>();
        entityMock.Setup.Id_Get().Returns(1);
        entityMock.Setup.CreatedBy_Get().Returns("system");
        entityMock.Setup.IsDeleted_Get().Returns(false);

        var repoMock = Mock.Of<IAuditRepository>();
        repoMock.Setup.GetByIdAsync(1).Returns(entityMock.Object);

        var activeList = new List<ISoftDeletable> { entityMock.Object };
        repoMock.Setup.GetActiveAsync().Returns((IReadOnlyList<ISoftDeletable>)activeList.AsReadOnly());

        // Act
        IAuditRepository repo = repoMock.Object;
        var fetched = await repo.GetByIdAsync(1);
        var actives = await repo.GetActiveAsync();

        // Assert
        await Assert.That(fetched).IsNotNull();
        await Assert.That(fetched.Id).IsEqualTo(1);
        await Assert.That(fetched.CreatedBy).IsEqualTo("system");
        await Assert.That(fetched.IsDeleted).IsFalse();

        await Assert.That(actives).IsNotNull();
        await Assert.That(actives).HasCount().EqualTo(1);
        await Assert.That(actives[0].Id).IsEqualTo(1);
    }
}

/// <summary>
/// Edge case tests for exception throwing — sync, async, sequential, and per-argument exceptions.
/// </summary>
public class ExceptionEdgeCaseTests
{
    [Test]
    public async Task Throws_Specific_Exception_With_Message()
    {
        // Arrange
        var mock = Mock.Of<IExternalApi>();
        mock.Setup.GetConfig("missing-key").Throws<KeyNotFoundException>();

        // Act & Assert
        IExternalApi api = mock.Object;
        var exception = Assert.Throws<KeyNotFoundException>(() =>
        {
            api.GetConfig("missing-key");
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Sequential_Throws_Then_Succeeds()
    {
        // Arrange — simulate a retry pattern: first call fails, second succeeds
        var mock = Mock.Of<IExternalApi>();
        mock.Setup.CallRemoteServiceAsync("https://api.example.com/data")
            .Throws<InvalidOperationException>()
            .Then()
            .Returns("success");

        // Act
        IExternalApi api = mock.Object;

        // First call: throws (faulted task)
        var firstTask = api.CallRemoteServiceAsync("https://api.example.com/data");
        await Assert.That(firstTask.IsFaulted).IsTrue();

        try
        {
            await firstTask;
            await Assert.That(false).IsTrue(); // should not reach here
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Second call: succeeds
        var result = await api.CallRemoteServiceAsync("https://api.example.com/data");
        await Assert.That(result).IsEqualTo("success");
    }

    [Test]
    public async Task Async_Throws_Returns_Faulted_Task_Not_Sync_Throw()
    {
        // Arrange
        var mock = Mock.Of<IExternalApi>();
        mock.Setup.CallRemoteServiceAsync(Arg.Any<string>()).Throws<TimeoutException>();

        // Act — the method should return a faulted task, NOT throw synchronously
        IExternalApi api = mock.Object;
        var task = api.CallRemoteServiceAsync("any-endpoint");

        // Assert — the task itself is faulted (not a synchronous exception)
        await Assert.That(task.IsFaulted).IsTrue();

        try
        {
            await task;
            await Assert.That(false).IsTrue(); // should not reach
        }
        catch (TimeoutException)
        {
            await Assert.That(true).IsTrue(); // Expected
        }
    }

    [Test]
    public async Task Different_Args_Different_Exceptions()
    {
        // Arrange
        var mock = Mock.Of<IExternalApi>();
        mock.Setup.GetConfig("arg-error").Throws<ArgumentException>();
        mock.Setup.GetConfig("timeout").Throws<TimeoutException>();
        mock.Setup.GetConfig("valid").Returns("value");

        // Act & Assert
        IExternalApi api = mock.Object;

        var httpEx = Assert.Throws<ArgumentException>(() => api.GetConfig("arg-error"));
        await Assert.That(httpEx).IsNotNull();

        var timeoutEx = Assert.Throws<TimeoutException>(() => api.GetConfig("timeout"));
        await Assert.That(timeoutEx).IsNotNull();

        var validResult = api.GetConfig("valid");
        await Assert.That(validResult).IsEqualTo("value");
    }
}

/// <summary>
/// Edge case tests for complex argument matching — predicates, multiple predicates,
/// precedence, and argument capture across many calls.
/// </summary>
public class ComplexArgumentMatchingTests
{
    [Test]
    public async Task Predicate_Matcher_String_Length_Range()
    {
        // Arrange
        var mock = Mock.Of<IValidator>();
        mock.Setup.Validate(Arg.Is<string>(s => s != null && s.Length >= 3 && s.Length <= 50)).Returns(true);

        // Act
        IValidator validator = mock.Object;

        // Assert — strings in range match
        await Assert.That(validator.Validate("abc")).IsTrue();
        await Assert.That(validator.Validate("a valid input string")).IsTrue();
        await Assert.That(validator.Validate(new string('x', 50))).IsTrue();

        // Strings out of range do not match (return default false)
        await Assert.That(validator.Validate("ab")).IsFalse();
        await Assert.That(validator.Validate("")).IsFalse();
        await Assert.That(validator.Validate(new string('x', 51))).IsFalse();
    }

    [Test]
    public async Task Multiple_Predicates_On_Multiple_Args()
    {
        // Arrange
        var mock = Mock.Of<IValidator>();
        mock.Setup.Score(
            Arg.Is<string>(t => t != null && t.Length > 0),
            Arg.Is<int>(w => w >= 1 && w <= 10)
        ).Returns(100);

        // Act
        IValidator validator = mock.Object;

        // Assert — both predicates satisfied
        await Assert.That(validator.Score("hello", 5)).IsEqualTo(100);
        await Assert.That(validator.Score("x", 1)).IsEqualTo(100);
        await Assert.That(validator.Score("test", 10)).IsEqualTo(100);

        // One or both predicates not satisfied — returns default (0)
        await Assert.That(validator.Score("", 5)).IsEqualTo(0);      // empty string fails
        await Assert.That(validator.Score("hello", 0)).IsEqualTo(0);  // weight out of range
        await Assert.That(validator.Score("hello", 11)).IsEqualTo(0); // weight out of range
    }

    [Test]
    public async Task Exact_Match_Overrides_Any_Match_Later_Wins()
    {
        // Arrange — setup Any first, then exact. Last setup wins for matching args.
        var mock = Mock.Of<IValidator>();
        mock.Setup.Validate(Arg.Any<string>()).Returns(false);
        mock.Setup.Validate("special").Returns(true);

        // Act
        IValidator validator = mock.Object;

        // Assert — "special" matches the exact setup (last wins), others match Any
        await Assert.That(validator.Validate("special")).IsTrue();
        await Assert.That(validator.Validate("anything else")).IsFalse();
        await Assert.That(validator.Validate("")).IsFalse();
    }

    [Test]
    public async Task Arg_Capture_Across_Multiple_Calls_Verifies_All()
    {
        // Arrange
        var capture = new ArgCapture<string>();
        var mock = Mock.Of<IValidator>();
        mock.Setup.Validate(Arg.Capture(capture)).Returns(true);

        // Act — 5 calls with different arguments
        IValidator validator = mock.Object;
        validator.Validate("alpha");
        validator.Validate("bravo");
        validator.Validate("charlie");
        validator.Validate("delta");
        validator.Validate("echo");

        // Assert — all captured
        await Assert.That(capture.Values).Count().IsEqualTo(5);
        await Assert.That(capture.Values[0]).IsEqualTo("alpha");
        await Assert.That(capture.Values[1]).IsEqualTo("bravo");
        await Assert.That(capture.Values[2]).IsEqualTo("charlie");
        await Assert.That(capture.Values[3]).IsEqualTo("delta");
        await Assert.That(capture.Values[4]).IsEqualTo("echo");
        await Assert.That(capture.Latest).IsEqualTo("echo");
    }
}

/// <summary>
/// Edge case tests for mock reset and reconfiguration mid-test.
/// </summary>
public class ResetReconfigurationTests
{
    [Test]
    public async Task Reset_Then_Reconfigure_Different_Behavior()
    {
        // Arrange
        var mock = Mock.Of<IExternalApi>();
        mock.Setup.GetConfig("key").Returns("A");

        IExternalApi api = mock.Object;

        // Act — first phase
        var firstResult = api.GetConfig("key");
        await Assert.That(firstResult).IsEqualTo("A");

        // Reset and reconfigure
        mock.Reset();
        mock.Setup.GetConfig("key").Returns("B");

        // Second phase
        var secondResult = api.GetConfig("key");

        // Assert
        await Assert.That(secondResult).IsEqualTo("B");
    }

    [Test]
    public async Task Reset_Mid_Test_Changes_Verification_Baseline()
    {
        // Arrange
        var mock = Mock.Of<IExternalApi>();
        IExternalApi api = mock.Object;

        // Act — make calls before reset
        api.GetConfig("key1");
        api.GetConfig("key2");
        api.GetConfig("key1");

        // Verify pre-reset state
        mock.Verify.GetConfig("key1").WasCalled(Times.Exactly(2));
        mock.Verify.GetConfig("key2").WasCalled(Times.Once);

        // Reset — clears call history
        mock.Reset();

        // Post-reset: previous calls should not count
        mock.Verify.GetConfig("key1").WasNeverCalled();
        mock.Verify.GetConfig("key2").WasNeverCalled();

        // Make new calls
        api.GetConfig("key1");

        // Verify only post-reset calls
        mock.Verify.GetConfig("key1").WasCalled(Times.Once);
        mock.Verify.GetConfig("key2").WasNeverCalled();
        await Assert.That(true).IsTrue();
    }
}

/// <summary>
/// Edge case tests for concurrent access — race conditions, parallel setup and invocation.
/// </summary>
public class ConcurrentAccessTests
{
    [Test]
    public async Task Concurrent_Setup_And_Invocation_From_Multiple_Threads()
    {
        // Arrange
        var mock = Mock.Of<IConcurrentService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns(42);
        IConcurrentService svc = mock.Object;

        // Act — 10 threads setting up and calling simultaneously
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            // Each thread sets up its own key and calls it
            var key = $"key-{i}";
            mock.Setup.GetValue(key).Returns(i);
            return svc.GetValue(key);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert — all tasks completed without exception, results are valid ints
        await Assert.That(results).HasCount().EqualTo(10);
        foreach (var result in results)
        {
            // Result is some valid int (either the thread-specific setup or the Any setup)
            await Assert.That(result).IsGreaterThanOrEqualTo(0);
        }
    }

    [Test]
    public async Task Concurrent_Verification_After_Parallel_Calls()
    {
        // Arrange
        var mock = Mock.Of<IConcurrentService>();
        mock.Setup.IncrementAsync(Arg.Any<string>()).Returns(1);
        IConcurrentService svc = mock.Object;

        // Act — 100 parallel calls
        var tasks = Enumerable.Range(0, 100).Select(_ =>
            Task.Run(async () => await svc.IncrementAsync("counter")));
        await Task.WhenAll(tasks);

        // Assert — verify exact count after all parallel calls complete
        mock.Verify.IncrementAsync("counter").WasCalled(Times.Exactly(100));
        await Assert.That(true).IsTrue();
    }
}

/// <summary>
/// Edge case tests for methods with enum parameters — exact match, predicate matchers, verification.
/// </summary>
public class EnumParameterTests
{
    [Test]
    public async Task Enum_Parameter_Exact_Match()
    {
        // Arrange
        var mock = Mock.Of<ITaskManager>();

        var lowItems = new List<TaskItem>
        {
            new() { Id = 1, Title = "Low task", Priority = Priority.Low, Status = Status.Pending }
        };
        var highItems = new List<TaskItem>
        {
            new() { Id = 2, Title = "High task", Priority = Priority.High, Status = Status.Active },
            new() { Id = 3, Title = "Another high", Priority = Priority.High, Status = Status.Active }
        };

        mock.Setup.GetByPriorityAsync(Priority.Low).Returns(lowItems);
        mock.Setup.GetByPriorityAsync(Priority.High).Returns(highItems);

        // Act
        ITaskManager mgr = mock.Object;
        var lowResult = await mgr.GetByPriorityAsync(Priority.Low);
        var highResult = await mgr.GetByPriorityAsync(Priority.High);

        // Assert
        await Assert.That(lowResult).HasCount().EqualTo(1);
        await Assert.That(lowResult[0].Title).IsEqualTo("Low task");

        await Assert.That(highResult).HasCount().EqualTo(2);
        await Assert.That(highResult[0].Title).IsEqualTo("High task");
    }

    [Test]
    public async Task Enum_Parameter_With_Predicate_Matcher()
    {
        // Arrange
        var mock = Mock.Of<ITaskManager>();
        mock.Setup.CountByStatusAsync(Arg.Is<Status>(s => s == Status.Active || s == Status.Pending)).Returns(10);
        mock.Setup.CountByStatusAsync(Arg.Is<Status>(s => s == Status.Completed || s == Status.Failed)).Returns(5);

        // Act
        ITaskManager mgr = mock.Object;

        // Assert — active/pending statuses return 10
        await Assert.That(await mgr.CountByStatusAsync(Status.Active)).IsEqualTo(10);
        await Assert.That(await mgr.CountByStatusAsync(Status.Pending)).IsEqualTo(10);

        // completed/failed statuses return 5
        await Assert.That(await mgr.CountByStatusAsync(Status.Completed)).IsEqualTo(5);
        await Assert.That(await mgr.CountByStatusAsync(Status.Failed)).IsEqualTo(5);
    }

    [Test]
    public async Task Enum_Void_Method_Verify_Specific_Value()
    {
        // Arrange
        var mock = Mock.Of<ITaskManager>();
        ITaskManager mgr = mock.Object;

        // Act
        mgr.UpdateStatus(1, Status.Active);
        mgr.UpdateStatus(2, Status.Completed);
        mgr.UpdateStatus(3, Status.Active);

        // Assert — verify UpdateStatus called with specific Status values
        mock.Verify.UpdateStatus(Arg.Any<int>(), Status.Active).WasCalled(Times.Exactly(2));
        mock.Verify.UpdateStatus(Arg.Any<int>(), Status.Completed).WasCalled(Times.Once);
        mock.Verify.UpdateStatus(Arg.Any<int>(), Status.Failed).WasNeverCalled();
        mock.Verify.UpdateStatus(Arg.Any<int>(), Status.Pending).WasNeverCalled();

        // Verify specific task ID + status combination
        mock.Verify.UpdateStatus(1, Status.Active).WasCalled(Times.Once);
        mock.Verify.UpdateStatus(2, Status.Completed).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }
}
