# Best Practices

This guide covers best practices for writing clean, maintainable, and robust tests with TUnit. Following these patterns will help you create a test suite that's easy to understand and maintain over time.

## Test Naming

### Use Descriptive Names

Good test names clearly describe what's being tested and what's expected. A common pattern is `Method_Scenario_ExpectedBehavior`:

```csharp
// ✅ Good: Clearly describes what's being tested
[Test]
public async Task CalculateTotal_WithDiscount_ReturnsReducedPrice()
{
    var calculator = new PriceCalculator();
    var result = calculator.CalculateTotal(100, discount: 0.2);
    await Assert.That(result).IsEqualTo(80);
}

// ❌ Bad: Vague and unclear
[Test]
public async Task Test1()
{
    var calculator = new PriceCalculator();
    var result = calculator.CalculateTotal(100, 0.2);
    await Assert.That(result).IsEqualTo(80);
}
```

### Alternative Naming Patterns

You can also use sentence-like names that read naturally:

```csharp
[Test]
public async Task When_discount_is_applied_total_is_reduced()
{
    // Test implementation
}

[Test]
public async Task Should_return_reduced_price_when_discount_applied()
{
    // Test implementation
}
```

Pick a naming convention and stick to it throughout your project for consistency.

## Test Organization

### One Test Class Per Production Class

Organize your tests to mirror your production code structure:

```
MyApp/
  Services/
    OrderService.cs
    PaymentService.cs

MyApp.Tests/
  Services/
    OrderServiceTests.cs
    PaymentServiceTests.cs
```

This makes it easy to find tests and keeps your test suite organized as your codebase grows.

### Group Related Tests

Use nested classes or separate test classes to group related test scenarios:

```csharp
public class OrderServiceTests
{
    public class CreateOrder
    {
        [Test]
        public async Task Creates_order_with_valid_data()
        {
            // Test implementation
        }

        [Test]
        public async Task Throws_exception_when_user_not_found()
        {
            // Test implementation
        }
    }

    public class CancelOrder
    {
        [Test]
        public async Task Cancels_order_successfully()
        {
            // Test implementation
        }

        [Test]
        public async Task Throws_when_order_already_shipped()
        {
            // Test implementation
        }
    }
}
```

### Keep Test Files Focused

Each test file should focus on testing a single class or component. If your test file is getting large (>500 lines), consider splitting it into multiple files or using nested classes.

## Assertion Best Practices

### Prefer Specific Assertions

Use the most specific assertion available for better failure messages:

```csharp
// ✅ Good: Specific assertion with clear failure message
await Assert.That(result).IsEqualTo(5);
// Failure: Expected 5 but was 3

// ❌ Okay but less helpful: Generic boolean assertion
await Assert.That(result == 5).IsTrue();
// Failure: Expected true but was false
```

### One Logical Assertion Per Test

Each test should verify one specific behavior. Multiple assertions are fine if they're testing different aspects of the same behavior:

```csharp
// ✅ Good: Multiple assertions testing one behavior (user creation)
[Test]
public async Task CreateUser_SetsAllProperties()
{
    var user = await userService.CreateUser("john@example.com", "John Doe");

    await Assert.That(user.Email).IsEqualTo("john@example.com");
    await Assert.That(user.Name).IsEqualTo("John Doe");
    await Assert.That(user.CreatedAt).IsNotEqualTo(default(DateTime));
}

// ❌ Bad: Testing multiple unrelated behaviors
[Test]
public async Task UserService_Works()
{
    var user = await userService.CreateUser("john@example.com", "John");
    await Assert.That(user.Email).IsEqualTo("john@example.com");

    await userService.DeleteUser(user.Id);
    var deleted = await userService.GetUser(user.Id);
    await Assert.That(deleted).IsNull();
}
```

### Always Await Assertions

TUnit assertions are async and must be awaited. Forgetting `await` means the assertion never runs:

```csharp
// ❌ Wrong: Assertion returns Task that's never awaited
[Test]
public async Task MyTest()
{
    Assert.That(result).IsEqualTo(5);  // Test passes without checking!
}

// ✅ Correct: Assertion is awaited and executed
[Test]
public async Task MyTest()
{
    await Assert.That(result).IsEqualTo(5);
}
```

The compiler will warn you about unawaited tasks, but watch for this common mistake.

## Test Lifecycle Management

### Use Hooks for Setup and Cleanup

TUnit provides several hooks for test lifecycle management. Use them to keep your test logic clean:

```csharp
public class DatabaseTests
{
    private TestDatabase? _database;

    [Before(Test)]
    public async Task SetupDatabase()
    {
        _database = await TestDatabase.CreateAsync();
    }

    [After(Test)]
    public async Task CleanupDatabase()
    {
        if (_database != null)
            await _database.DisposeAsync();
    }

    [Test]
    public async Task Can_insert_record()
    {
        // Database is ready to use
        await _database!.InsertAsync(new Record { Id = 1 });
        var result = await _database.GetAsync(1);
        await Assert.That(result).IsNotNull();
    }
}
```

### Choose the Right Hook Level

- `[Before(Test)]` / `[After(Test)]`: Runs before/after each test (most common)
- `[Before(Class)]` / `[After(Class)]`: Runs once per test class
- `[Before(Assembly)]` / `[After(Assembly)]`: Runs once per test assembly

### Sharing Expensive Resources

For expensive setup that needs to be shared across tests (like web servers, databases, or containers), use `[ClassDataSource<>]` with shared types and `IAsyncInitializer`/`IAsyncDisposable`:

```csharp
// ✅ Best: Shared resource with ClassDataSource
public class TestWebServer : IAsyncInitializer, IAsyncDisposable
{
    public WebApplicationFactory<Program>? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>();
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (Factory != null)
            await Factory.DisposeAsync();
    }
}

[ClassDataSource<TestWebServer>(Shared = SharedType.PerTestSession)]
public class ApiTests(TestWebServer server)
{
    [Test]
    public async Task Can_call_endpoint()
    {
        var client = server.Factory!.CreateClient();
        var response = await client.GetAsync("/api/health");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task Can_get_users()
    {
        var client = server.Factory!.CreateClient();
        var response = await client.GetAsync("/api/users");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}
```

**Why this is better:**
- Keeps test files simpler (no static fields or Before/After hooks)
- Shared resources work across multiple test classes
- Can share across assemblies using `SharedType.PerTestSession`
- Cleaner lifecycle management with `IAsyncInitializer`/`IAsyncDisposable`
- Type-safe dependency injection into test constructors

**Shared Type Options:**
- `SharedType.PerTestSession`: One instance for entire test run, shared across assemblies (best for expensive resources)
- `SharedType.PerClass`: One instance per test class
- `SharedType.None`: New instance per test (default)

You can also use hooks, but they're less flexible:

```csharp
// ❌ Less flexible: Using hooks for shared setup
public class ApiTests
{
    private static WebApplicationFactory<Program>? _factory;

    [Before(Class)]
    public static async Task StartServer()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [After(Class)]
    public static async Task StopServer()
    {
        _factory?.Dispose();
    }

    // Tests use static _factory field
}
```

### Avoid Complex Setup Logic

Keep your setup code simple and focused. If setup is complex, extract it to helper methods:

```csharp
// ✅ Good: Simple setup with extracted helpers
[Before(Test)]
public async Task Setup()
{
    _database = await CreateTestDatabase();
    _testUser = await CreateTestUser();
}

private async Task<TestDatabase> CreateTestDatabase()
{
    var db = await TestDatabase.CreateAsync();
    await db.SeedDefaultData();
    return db;
}

// ❌ Bad: Complex setup logic in hook
[Before(Test)]
public async Task Setup()
{
    _database = await TestDatabase.CreateAsync();
    await _database.ExecuteAsync("CREATE TABLE Users (...)");
    await _database.ExecuteAsync("INSERT INTO Users VALUES (...)");
    await _database.ExecuteAsync("CREATE TABLE Orders (...)");
    // ... lots more setup code
}
```

## Parallelism Guidance

### Tests Run in Parallel By Default

TUnit runs tests in parallel for better performance. Write your tests to be independent:

```csharp
// ✅ Good: Test is self-contained and independent
[Test]
public async Task Can_create_order()
{
    var orderId = Guid.NewGuid();  // Unique ID
    var order = new Order { Id = orderId, Total = 100 };
    await orderService.CreateAsync(order);

    var result = await orderService.GetAsync(orderId);
    await Assert.That(result).IsNotNull();
}
```

### Use NotInParallel When Needed

Some tests can't run in parallel (database tests, file system tests). Use `[NotInParallel]`:

```csharp
// Tests that modify shared state
[Test, NotInParallel]
public async Task Updates_configuration_file()
{
    await ConfigurationManager.SetAsync("key", "value");
    var result = await ConfigurationManager.GetAsync("key");
    await Assert.That(result).IsEqualTo("value");
}
```

### Control Execution Order

When tests need to run in a specific order, use `[DependsOn]` instead of `NotInParallel` with `Order`:

```csharp
// ✅ Good: Use DependsOn for ordering while maintaining parallelism
[Test]
public async Task Step1_CreateUser()
{
    // Runs first
}

[Test]
[DependsOn(nameof(Step1_CreateUser))]
public async Task Step2_UpdateUser()
{
    // Runs after Step1_CreateUser completes
    // Other unrelated tests can still run in parallel
}

[Test]
[DependsOn(nameof(Step2_UpdateUser))]
public async Task Step3_DeleteUser()
{
    // Runs after Step2_UpdateUser completes
}
```

**Why `[DependsOn]` is better:**
- More intuitive: explicitly declares dependencies between tests
- More flexible: tests can depend on multiple other tests
- Maintains parallelism: unrelated tests still run in parallel
- Better for complex workflows: clear dependency chains

You can also use `NotInParallel` with `Order`, but this forces sequential execution:

```csharp
// ❌ Less flexible: Forces all tests to run sequentially
[Test, NotInParallel(Order = 1)]
public async Task Step1_CreateUser()
{
    // Runs first
}

[Test, NotInParallel(Order = 2)]
public async Task Step2_UpdateUser()
{
    // Runs second, but blocks all other tests
}
```

**Important:** If tests need ordering, they might be too tightly coupled. Consider:
- Refactoring into a single test
- Using proper setup/teardown
- Making tests truly independent

### Use Parallel Groups

Group related tests that can't run in parallel with each other but can run in parallel with other groups:

```csharp
public class FileSystemTests
{
    // These tests can't run in parallel with each other
    // but can run in parallel with DatabaseTests

    [Test, NotInParallel("FileGroup")]
    public async Task Test1_WritesFile()
    {
        // Test implementation
    }

    [Test, NotInParallel("FileGroup")]
    public async Task Test2_ReadsFile()
    {
        // Test implementation
    }
}

public class DatabaseTests
{
    [Test, NotInParallel("DbGroup")]
    public async Task Test1_InsertsRecord()
    {
        // Runs in parallel with FileSystemTests
    }
}
```

## Common Anti-Patterns to Avoid

### Avoid Test Interdependence

Each test should be completely independent and not rely on other tests:

```csharp
// ❌ Bad: Tests depend on execution order
private static User? _user;

[Test]
public async Task Test1_CreateUser()
{
    _user = await userService.CreateAsync("john@example.com");
}

[Test]
public async Task Test2_UpdateUser()
{
    // Assumes Test1 ran first!
    _user!.Name = "Jane Doe";
    await userService.UpdateAsync(_user);
}

// ✅ Good: Each test is independent
[Test]
public async Task Can_create_user()
{
    var user = await userService.CreateAsync("john@example.com");
    await Assert.That(user.Email).IsEqualTo("john@example.com");
}

[Test]
public async Task Can_update_user()
{
    var user = await userService.CreateAsync("jane@example.com");
    user.Name = "Jane Doe";
    await userService.UpdateAsync(user);

    var updated = await userService.GetAsync(user.Id);
    await Assert.That(updated.Name).IsEqualTo("Jane Doe");
}
```

### Avoid Shared Instance State

**Important:** TUnit creates a new instance of your test class for each test method. Don't rely on instance fields to share state:

```csharp
// ❌ Bad: Trying to share instance state between tests
public class MyTests
{
    private int _value;  // Different instance per test!

    [Test, NotInParallel]
    public void Test1()
    {
        _value = 99;
    }

    [Test, NotInParallel]
    public async Task Test2()
    {
        await Assert.That(_value).IsEqualTo(99);  // Fails! _value is 0
    }
}

// ✅ Good: Use static fields if you really need shared state
public class MyTests
{
    private static int _value;  // Shared across all tests

    [Test, NotInParallel]
    public void Test1()
    {
        _value = 99;
    }

    [Test, NotInParallel]
    public async Task Test2()
    {
        await Assert.That(_value).IsEqualTo(99);  // Works!
    }
}
```

But seriously: if tests need to share state, reconsider your design. It's usually better to make tests independent.

### Avoid Complex Test Logic

Tests should be simple and easy to understand. Avoid complex conditionals, loops, or calculations:

```csharp
// ❌ Bad: Complex logic in test
[Test]
public async Task CalculatesTotals()
{
    var items = await GetItems();
    decimal expected = 0;
    foreach (var item in items)
    {
        if (item.IsDiscounted)
            expected += item.Price * 0.8m;
        else
            expected += item.Price;
    }

    var result = calculator.CalculateTotal(items);
    await Assert.That(result).IsEqualTo(expected);
}

// ✅ Good: Simple, explicit test
[Test]
public async Task CalculateTotal_WithMixedItems()
{
    var items = new[]
    {
        new Item { Price = 100, IsDiscounted = false },  // 100
        new Item { Price = 50, IsDiscounted = true }     // 40
    };

    var result = calculator.CalculateTotal(items);
    await Assert.That(result).IsEqualTo(140);
}
```

If your test has complex logic, you're essentially writing code to test code. Keep it simple!

### Avoid Over-Mocking

Don't mock everything. Use real implementations when they're fast and reliable:

```csharp
// ❌ Bad: Mocking things that don't need mocking
[Test]
public async Task ProcessOrder()
{
    var mockLogger = new Mock<ILogger>();
    var mockValidator = new Mock<IValidator>();
    var mockCalculator = new Mock<IPriceCalculator>();
    var mockRepository = new Mock<IOrderRepository>();

    // So much setup...
}

// ✅ Good: Only mock expensive or external dependencies
[Test]
public async Task ProcessOrder()
{
    var logger = new NullLogger();  // Real lightweight implementation
    var validator = new OrderValidator();  // Real validator is fast
    var calculator = new PriceCalculator();  // Simple calculations
    var mockRepository = new Mock<IOrderRepository>();  // Mock database

    // Much simpler!
}
```

Mock external dependencies (databases, APIs, file systems) but use real implementations for simple logic.

### Avoid Testing Implementation Details

Test behavior, not implementation. Your tests should verify what the code does, not how it does it:

```csharp
// ❌ Bad: Testing internal implementation
[Test]
public async Task ProcessOrder_CallsRepositorySaveMethod()
{
    var mockRepository = new Mock<IOrderRepository>();
    var service = new OrderService(mockRepository.Object);

    await service.ProcessOrder(order);

    // Verifying method calls instead of behavior
    mockRepository.Verify(r => r.Save(It.IsAny<Order>()), Times.Once);
}

// ✅ Good: Testing actual behavior
[Test]
public async Task ProcessOrder_SavesOrderToDatabase()
{
    var repository = new InMemoryOrderRepository();
    var service = new OrderService(repository);

    await service.ProcessOrder(order);

    // Verifying the result
    var saved = await repository.GetAsync(order.Id);
    await Assert.That(saved).IsNotNull();
    await Assert.That(saved.Status).IsEqualTo(OrderStatus.Processed);
}
```

Tests that verify implementation details are brittle and break when you refactor.

## Performance Considerations

TUnit is designed for performance at scale. Follow these guidelines to keep your test suite fast:

### Optimize Test Discovery

- Use AOT mode for faster test discovery and lower memory usage
- Keep data sources lightweight (see [Performance Best Practices](../advanced/performance-best-practices.md))
- Limit matrix test combinations to avoid test explosion

### Optimize Test Execution

- Let tests run in parallel (it's fast!)
- Only use `[NotInParallel]` when absolutely necessary
- Configure parallelism using the `--maximum-parallel-tests` CLI flag or `[assembly: ParallelLimiter<T>]` attribute
- Avoid expensive setup in `[Before(Test)]` hooks - use class or assembly-level hooks for shared resources

### Avoid Slow Operations in Tests

Tests should be fast. If a test takes more than a few seconds, look for optimization opportunities:

```csharp
// ❌ Slow: Real HTTP calls
[Test]
public async Task GetUserData()
{
    var client = new HttpClient();
    var response = await client.GetAsync("https://api.example.com/users");
    // Slow and unreliable
}

// ✅ Fast: Use in-memory test doubles
[Test]
public async Task GetUserData()
{
    var client = new TestHttpClient();  // In-memory fake
    var response = await client.GetAsync("/users");
    // Fast and reliable
}
```

For detailed performance guidance, see [Performance Best Practices](../advanced/performance-best-practices.md).

## Summary

Following these best practices will help you:

- Write tests that are easy to understand and maintain
- Create a fast, reliable test suite that scales
- Catch bugs without introducing brittle tests
- Make your codebase more maintainable over time

Remember: good tests are simple, focused, independent, and fast. When in doubt, ask yourself: "Will someone else understand what this test is doing and why it might fail?"
