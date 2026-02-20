using TUnit.Mock;
using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Tests;

// ───────────────────────────────────────────────────────────────
// Supporting DTOs
// ───────────────────────────────────────────────────────────────

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class OrderDto
{
    public int OrderId { get; set; }
    public string ItemName { get; set; } = "";
    public decimal Price { get; set; }
}

// ───────────────────────────────────────────────────────────────
// 1. Service Layer Pattern — Repository + Unit of Work
// ───────────────────────────────────────────────────────────────

public interface IUserRepository
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(UserDto user);
    Task UpdateAsync(UserDto user);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<IReadOnlyList<UserDto>> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<ITransaction> BeginTransactionAsync();
}

public interface ITransaction : IDisposable, IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}

// ───────────────────────────────────────────────────────────────
// 2. Multi-Interface Service
// ───────────────────────────────────────────────────────────────

public interface ILogger
{
    void Log(string level, string message);
    void Log(string level, string message, Exception? exception);
}

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task<bool> SendSmsAsync(string phoneNumber, string message);
}

public interface ICache
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    bool Remove(string key);
    bool Exists(string key);
}

// ───────────────────────────────────────────────────────────────
// 3. Complex Return Types
// ───────────────────────────────────────────────────────────────

public interface IAnalyticsService
{
    Task<Dictionary<string, List<decimal>>> GetMetricsAsync(string category);
    (bool Success, string? ErrorMessage) Validate(string input);
    Task<IReadOnlyDictionary<int, string>> GetLookupAsync();
}

// ───────────────────────────────────────────────────────────────
// 5. Nullable Reference Types
// ───────────────────────────────────────────────────────────────

public interface INullableService
{
    string? GetNullableString(int id);
    Task<UserDto?> FindUserAsync(string? name);
    void Process(string? input, int? count);
}

// ───────────────────────────────────────────────────────────────
// 4. Dependency Injection Integration — Real Service
// ───────────────────────────────────────────────────────────────

public class OrderService
{
    private readonly IUserRepository _userRepo;
    private readonly INotificationService _notifications;
    private readonly ILogger _logger;

    public OrderService(IUserRepository userRepo, INotificationService notifications, ILogger logger)
    {
        _userRepo = userRepo;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<bool> PlaceOrderAsync(int userId, string itemName)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.Log("Warning", $"User {userId} not found");
            return false;
        }

        await _notifications.SendEmailAsync(user.Email, "Order Placed", $"Your order for {itemName} has been placed.");
        _logger.Log("Info", $"Order placed for user {userId}");
        return true;
    }
}

// ═══════════════════════════════════════════════════════════════
// Test Class
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Real-world mocking scenario tests that exercise complex patterns
/// commonly found in production codebases: repository/UoW, DI orchestration,
/// multi-interface coordination, complex return types, and nullable handling.
/// </summary>
public class RealWorldScenarioTests
{
    // ───────────────────────────────────────────────────────────
    // 1. Service Layer Pattern
    // ───────────────────────────────────────────────────────────

    [Test]
    public async Task Repository_CRUD_Full_Lifecycle()
    {
        // Arrange
        var mock = Mock.Of<IUserRepository>();
        var user = new UserDto { Id = 1, Name = "Alice", Email = "alice@example.com" };

        mock.Setup.CreateAsync(Arg.Any<UserDto>()).Returns(user);
        mock.Setup.GetByIdAsync(1).Returns(user);
        mock.Setup.ExistsAsync(1).Returns(true);

        IUserRepository repo = mock.Object;

        // Act — Create
        var created = await repo.CreateAsync(user);
        await Assert.That(created.Name).IsEqualTo("Alice");

        // Act — Read back
        var fetched = await repo.GetByIdAsync(1);
        await Assert.That(fetched).IsNotNull();
        await Assert.That(fetched!.Email).IsEqualTo("alice@example.com");

        // Act — Update (void Task, just call it)
        var updatedUser = new UserDto { Id = 1, Name = "Alice Updated", Email = "alice@example.com" };
        await repo.UpdateAsync(updatedUser);

        // Act — Exists
        var exists = await repo.ExistsAsync(1);
        await Assert.That(exists).IsTrue();

        // Verify all calls
        mock.Verify.CreateAsync(Arg.Any<UserDto>()).WasCalled(Times.Once);
        mock.Verify.GetByIdAsync(1).WasCalled(Times.Once);
        mock.Verify.UpdateAsync(Arg.Any<UserDto>()).WasCalled(Times.Once);
        mock.Verify.ExistsAsync(1).WasCalled(Times.Once);
    }

    [Test]
    public async Task Repository_With_CancellationToken_Default_Parameter()
    {
        // Arrange
        var mock = Mock.Of<IUserRepository>();
        var users = new List<UserDto>
        {
            new() { Id = 1, Name = "Alice Smith", Email = "alice@example.com" },
            new() { Id = 2, Name = "Alice Jones", Email = "alicej@example.com" },
        };
        mock.Setup.FindByNameAsync("Alice", Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<UserDto>)users);

        IUserRepository repo = mock.Object;

        // Act — call without CancellationToken (uses default)
        var result = await repo.FindByNameAsync("Alice");

        // Assert
        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result[0].Name).IsEqualTo("Alice Smith");

        // Verify
        mock.Verify.FindByNameAsync("Alice", Arg.Any<CancellationToken>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task UnitOfWork_Transaction_Commit_Flow()
    {
        // Arrange
        var mockTx = Mock.Of<ITransaction>();
        var mockUow = Mock.Of<IUnitOfWork>();
        mockUow.Setup.BeginTransactionAsync().Returns(mockTx.Object);

        IUnitOfWork uow = mockUow.Object;

        // Act — simulate a transactional operation
        var tx = await uow.BeginTransactionAsync();
        await uow.SaveChangesAsync();
        await tx.CommitAsync();

        // Assert
        mockUow.Verify.BeginTransactionAsync().WasCalled(Times.Once);
        mockUow.Verify.SaveChangesAsync(Arg.Any<CancellationToken>()).WasCalled(Times.Once);
        mockTx.Verify.CommitAsync().WasCalled(Times.Once);
        mockTx.Verify.RollbackAsync().WasNeverCalled();
    }

    [Test]
    public async Task UnitOfWork_Transaction_Rollback_On_Exception()
    {
        // Arrange
        var mockTx = Mock.Of<ITransaction>();
        var mockUow = Mock.Of<IUnitOfWork>();
        mockUow.Setup.BeginTransactionAsync().Returns(mockTx.Object);
        mockUow.Setup.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws<InvalidOperationException>();

        IUnitOfWork uow = mockUow.Object;

        // Act — simulate a transactional operation that fails
        var tx = await uow.BeginTransactionAsync();
        try
        {
            await uow.SaveChangesAsync();
        }
        catch (InvalidOperationException)
        {
            await tx.RollbackAsync();
        }

        // Assert — rollback was called, commit was not
        mockTx.Verify.RollbackAsync().WasCalled(Times.Once);
        mockTx.Verify.CommitAsync().WasNeverCalled();
    }

    [Test]
    public async Task Repository_Returns_Null_For_NotFound()
    {
        // Arrange
        var mock = Mock.Of<IUserRepository>();
        mock.Setup.GetByIdAsync(999).Returns((UserDto?)null);

        IUserRepository repo = mock.Object;

        // Act
        var result = await repo.GetByIdAsync(999);

        // Assert
        await Assert.That(result).IsNull();
    }

    // ───────────────────────────────────────────────────────────
    // 2. Multi-Interface Service
    // ───────────────────────────────────────────────────────────

    [Test]
    public async Task Multiple_Mocks_Injected_Into_Service_Orchestration()
    {
        // Arrange — create 3 mocks that a hypothetical service would depend on
        var mockRepo = Mock.Of<IUserRepository>();
        var mockNotify = Mock.Of<INotificationService>();
        var mockLogger = Mock.Of<ILogger>();

        var user = new UserDto { Id = 1, Name = "Bob", Email = "bob@example.com" };
        mockRepo.Setup.GetByIdAsync(1).Returns(user);
        // Act — orchestrate the mocks together as a service would
        IUserRepository repo = mockRepo.Object;
        INotificationService notify = mockNotify.Object;
        ILogger logger = mockLogger.Object;

        var fetchedUser = await repo.GetByIdAsync(1);
        await Assert.That(fetchedUser).IsNotNull();

        await notify.SendEmailAsync(fetchedUser!.Email, "Welcome", "Hello Bob!");
        logger.Log("Info", "Welcome email sent");

        // Verify all three mocks
        mockRepo.Verify.GetByIdAsync(1).WasCalled(Times.Once);
        mockNotify.Verify.SendEmailAsync("bob@example.com", "Welcome", "Hello Bob!").WasCalled(Times.Once);
        mockLogger.Verify.Log("Info", "Welcome email sent").WasCalled(Times.Once);
    }

    [Test]
    public async Task Logger_Method_Overloads_Distinct_Setups()
    {
        // Arrange
        var mock = Mock.Of<ILogger>();
        var twoArgCallCount = 0;
        var threeArgCallCount = 0;

        mock.Setup.Log(Arg.Any<string>(), Arg.Any<string>())
            .Callback((Action)(() => twoArgCallCount++));
        mock.Setup.Log(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception?>())
            .Callback((Action)(() => threeArgCallCount++));

        ILogger logger = mock.Object;

        // Act — call each overload
        logger.Log("Info", "simple message");
        logger.Log("Error", "something failed", new InvalidOperationException("boom"));

        // Assert — each overload was tracked independently
        await Assert.That(twoArgCallCount).IsEqualTo(1);
        await Assert.That(threeArgCallCount).IsEqualTo(1);

        // Verify
        mock.Verify.Log(Arg.Any<string>(), Arg.Any<string>()).WasCalled(Times.Once);
        mock.Verify.Log(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Exception?>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Cache_Generic_Methods_With_Different_Types()
    {
        // Arrange
        var mock = Mock.Of<ICache>();
        var user = new UserDto { Id = 1, Name = "Alice", Email = "alice@example.com" };
        var order = new OrderDto { OrderId = 42, ItemName = "Widget", Price = 9.99m };

        mock.Setup.Get<UserDto>("user:1").Returns(user);
        mock.Setup.Get<OrderDto>("order:42").Returns(order);

        ICache cache = mock.Object;

        // Act
        var cachedUser = cache.Get<UserDto>("user:1");
        var cachedOrder = cache.Get<OrderDto>("order:42");

        // Assert — different generic types return different values
        await Assert.That(cachedUser).IsNotNull();
        await Assert.That(cachedUser!.Name).IsEqualTo("Alice");
        await Assert.That(cachedOrder).IsNotNull();
        await Assert.That(cachedOrder!.OrderId).IsEqualTo(42);

        // Unconfigured key returns null
        var missing = cache.Get<UserDto>("user:999");
        await Assert.That(missing).IsNull();
    }

    [Test]
    public async Task Notification_Conditional_Send()
    {
        // Arrange — SendSmsAsync returns true for one number, false for another
        var mock = Mock.Of<INotificationService>();
        mock.Setup.SendSmsAsync("+1234567890", Arg.Any<string>()).Returns(true);
        mock.Setup.SendSmsAsync("+0000000000", Arg.Any<string>()).Returns(false);

        INotificationService notify = mock.Object;

        // Act
        var successResult = await notify.SendSmsAsync("+1234567890", "Hello!");
        var failResult = await notify.SendSmsAsync("+0000000000", "Hello!");

        // Assert
        await Assert.That(successResult).IsTrue();
        await Assert.That(failResult).IsFalse();
    }

    // ───────────────────────────────────────────────────────────
    // 3. Complex Return Types
    // ───────────────────────────────────────────────────────────

    [Test]
    public async Task Complex_Dictionary_Return_Type()
    {
        // Arrange
        var mock = Mock.Of<IAnalyticsService>();
        var metrics = new Dictionary<string, List<decimal>>
        {
            ["revenue"] = [100.50m, 200.75m, 300.00m],
            ["costs"] = [50.25m, 75.00m],
        };
        mock.Setup.GetMetricsAsync("finance").Returns(metrics);

        IAnalyticsService analytics = mock.Object;

        // Act
        var result = await analytics.GetMetricsAsync("finance");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.ContainsKey("revenue")).IsTrue();
        await Assert.That(result["revenue"]).Count().IsEqualTo(3);
        await Assert.That(result["revenue"][0]).IsEqualTo(100.50m);
        await Assert.That(result["costs"]).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Tuple_Return_Type()
    {
        // Arrange
        var mock = Mock.Of<IAnalyticsService>();
        mock.Setup.Validate("good-input").Returns((true, (string?)null));
        mock.Setup.Validate("bad-input").Returns((false, (string?)"Invalid format"));

        IAnalyticsService analytics = mock.Object;

        // Act
        var goodResult = analytics.Validate("good-input");
        var badResult = analytics.Validate("bad-input");

        // Assert
        await Assert.That(goodResult.Success).IsTrue();
        await Assert.That(goodResult.ErrorMessage).IsNull();
        await Assert.That(badResult.Success).IsFalse();
        await Assert.That(badResult.ErrorMessage).IsEqualTo("Invalid format");
    }

    [Test]
    public async Task ReadOnlyDictionary_Return()
    {
        // Arrange
        var mock = Mock.Of<IAnalyticsService>();
        var lookup = new Dictionary<int, string>
        {
            [1] = "Active",
            [2] = "Inactive",
            [3] = "Pending",
        };
        mock.Setup.GetLookupAsync()
            .Returns((IReadOnlyDictionary<int, string>)lookup);

        IAnalyticsService analytics = mock.Object;

        // Act
        var result = await analytics.GetLookupAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result[1]).IsEqualTo("Active");
        await Assert.That(result[2]).IsEqualTo("Inactive");
        await Assert.That(result[3]).IsEqualTo("Pending");
    }

    // ───────────────────────────────────────────────────────────
    // 4. Dependency Injection Integration Pattern
    // ───────────────────────────────────────────────────────────

    [Test]
    public async Task DI_Integration_Happy_Path()
    {
        // Arrange
        var mockRepo = Mock.Of<IUserRepository>();
        var mockNotify = Mock.Of<INotificationService>();
        var mockLogger = Mock.Of<ILogger>();

        var user = new UserDto { Id = 1, Name = "Alice", Email = "alice@example.com" };
        mockRepo.Setup.GetByIdAsync(1).Returns(user);

        var service = new OrderService(mockRepo.Object, mockNotify.Object, mockLogger.Object);

        // Act
        var result = await service.PlaceOrderAsync(1, "Widget");

        // Assert
        await Assert.That(result).IsTrue();

        // Verify the repo was queried
        mockRepo.Verify.GetByIdAsync(1).WasCalled(Times.Once);

        // Verify email was sent to the correct address
        mockNotify.Verify.SendEmailAsync(
            "alice@example.com",
            "Order Placed",
            "Your order for Widget has been placed."
        ).WasCalled(Times.Once);

        // Verify info was logged
        mockLogger.Verify.Log("Info", "Order placed for user 1").WasCalled(Times.Once);
    }

    [Test]
    public async Task DI_Integration_User_Not_Found()
    {
        // Arrange
        var mockRepo = Mock.Of<IUserRepository>();
        var mockNotify = Mock.Of<INotificationService>();
        var mockLogger = Mock.Of<ILogger>();

        mockRepo.Setup.GetByIdAsync(999).Returns((UserDto?)null);

        var service = new OrderService(mockRepo.Object, mockNotify.Object, mockLogger.Object);

        // Act
        var result = await service.PlaceOrderAsync(999, "Widget");

        // Assert — returns false
        await Assert.That(result).IsFalse();

        // Verify warning was logged
        mockLogger.Verify.Log("Warning", "User 999 not found").WasCalled(Times.Once);

        // Verify email was NEVER sent
        mockNotify.Verify.SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()
        ).WasNeverCalled();
    }

    [Test]
    public async Task DI_Integration_Verify_Exact_Email_Content()
    {
        // Arrange
        var mockRepo = Mock.Of<IUserRepository>();
        var mockNotify = Mock.Of<INotificationService>();
        var mockLogger = Mock.Of<ILogger>();

        var captureBody = new ArgCapture<string>();

        var user = new UserDto { Id = 7, Name = "Charlie", Email = "charlie@example.com" };
        mockRepo.Setup.GetByIdAsync(7).Returns(user);
        // SendEmailAsync returns Task (void-async), so use Callback to capture args
        mockNotify.Setup.SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Capture(captureBody)
        ).Callback((Action)(() => { }));

        var service = new OrderService(mockRepo.Object, mockNotify.Object, mockLogger.Object);

        // Act
        await service.PlaceOrderAsync(7, "Gadget");

        // Assert — verify the captured email body content
        await Assert.That(captureBody.Values).HasCount().EqualTo(1);
        await Assert.That(captureBody.Latest).IsEqualTo("Your order for Gadget has been placed.");
    }

    // ───────────────────────────────────────────────────────────
    // 5. Nullable Reference Types
    // ───────────────────────────────────────────────────────────

    [Test]
    public async Task Nullable_String_Return_Configured_Null()
    {
        // Arrange
        var mock = Mock.Of<INullableService>();
        mock.Setup.GetNullableString(1).Returns((string?)null);

        INullableService svc = mock.Object;

        // Act
        var result = svc.GetNullableString(1);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Nullable_Parameter_Matching()
    {
        // Arrange
        var mock = Mock.Of<INullableService>();
        var callCount = 0;
        mock.Setup.Process(Arg.IsNull<string>(), Arg.Any<int?>())
            .Callback((Action)(() => callCount++));
        mock.Setup.Process(Arg.IsNotNull<string>(), Arg.Any<int?>())
            .Callback((Action)(() => callCount += 10));

        INullableService svc = mock.Object;

        // Act — call with null input
        svc.Process(null, 5);
        // Act — call with non-null input
        svc.Process("hello", null);

        // Assert — null path: +1, non-null path: +10
        await Assert.That(callCount).IsEqualTo(11);
    }

    [Test]
    public async Task Nullable_Async_Return()
    {
        // Arrange
        var mock = Mock.Of<INullableService>();
        mock.Setup.FindUserAsync(Arg.Any<string?>()).Returns((UserDto?)null);

        INullableService svc = mock.Object;

        // Act
        var result = await svc.FindUserAsync("nonexistent");

        // Assert
        await Assert.That(result).IsNull();

        // Now configure to return a user for a specific name
        mock.Setup.FindUserAsync("Alice")
            .Returns((UserDto?)new UserDto { Id = 1, Name = "Alice", Email = "alice@example.com" });

        var found = await svc.FindUserAsync("Alice");
        await Assert.That(found).IsNotNull();
        await Assert.That(found!.Name).IsEqualTo("Alice");
    }
}
