# Testing Cookbook

This cookbook provides practical, copy-paste examples for common testing scenarios with TUnit. Each recipe is a complete, working example you can adapt for your own tests.

## Dependency Injection Testing

### Testing with Microsoft.Extensions.DependencyInjection

```csharp
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

public class UserServiceTests
{
    private ServiceProvider? _serviceProvider;
    private IUserService? _userService;

    [Before(Test)]
    public async Task Setup()
    {
        // Create a service collection and register dependencies
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
    }

    [After(Test)]
    public async Task Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task CreateUser_RegistersNewUser()
    {
        var email = "test@example.com";
        var name = "Test User";

        var user = await _userService!.CreateAsync(email, name);

        await Assert.That(user.Email).IsEqualTo(email);
        await Assert.That(user.Name).IsEqualTo(name);
        await Assert.That(user.Id).IsNotEqualTo(Guid.Empty);
    }
}
```

### Testing with Scoped Services

```csharp
public class OrderServiceTests
{
    private ServiceProvider? _serviceProvider;

    [Before(Test)]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddDbContext<OrderDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        services.AddScoped<IOrderService, OrderService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [After(Test)]
    public async Task Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task CreateOrder_UsesScop​edDbContext()
    {
        // Create a scope for this test
        using var scope = _serviceProvider!.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var order = await orderService.CreateAsync(new CreateOrderRequest
        {
            CustomerId = 1,
            Items = new[] { new OrderItem { ProductId = 1, Quantity = 2 } }
        });

        await Assert.That(order.Id).IsGreaterThan(0);
        await Assert.That(order.Items).HasCount().EqualTo(1);
    }
}
```

## API Testing with WebApplicationFactory

### Testing a Minimal API Endpoint (Shared Server)

For API tests, it's more efficient to share a single WebApplicationFactory across all tests:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using TUnit.Core;

// Shared web server for all API tests
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
public class UserApiTests(TestWebServer server)
{
    [Test]
    public async Task GetUsers_ReturnsSuccessStatus()
    {
        var client = server.Factory!.CreateClient();
        var response = await client.GetAsync("/api/users");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task CreateUser_ReturnsCreatedUser()
    {
        var client = server.Factory!.CreateClient();
        var newUser = new CreateUserRequest
        {
            Email = "test@example.com",
            Name = "Test User"
        };

        var response = await client.PostAsJsonAsync("/api/users", newUser);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.Email).IsEqualTo(newUser.Email);
    }

    [Test]
    public async Task GetUser_WithInvalidId_ReturnsNotFound()
    {
        var client = server.Factory!.CreateClient();
        var response = await client.GetAsync("/api/users/99999");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
```

### Testing with Authentication (Per-Test Setup)

When each test needs different configuration (like different auth setups), use per-test hooks:

```csharp
public class AuthenticatedApiTests
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    [Before(Test)]
    public async Task Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Configure test authentication
                    services.AddAuthentication("Test")
                        .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                            "Test", options => { });
                });
            });

        _client = _factory.CreateClient();

        // Add auth header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
    }

    [After(Test)]
    public async Task Cleanup()
    {
        _client?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();
    }

    [Test]
    public async Task GetProfile_WithAuth_ReturnsUserProfile()
    {
        var response = await _client!.GetAsync("/api/profile");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfile>();
        await Assert.That(profile).IsNotNull();
    }

    [Test]
    public async Task GetProfile_WithoutAuth_ReturnsUnauthorized()
    {
        _client!.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/profile");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
```

> **Tip**: Use per-test setup (hooks) when tests need different configurations. Use shared setup (`ClassDataSource` with `SharedType.PerTestSession`) when all tests can use the same configuration.

## Mocking Patterns

### Using Moq

```csharp
using Moq;
using TUnit.Core;

public class OrderServiceMoqTests
{
    [Test]
    public async Task ProcessOrder_CallsPaymentService()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentService>();
        mockPaymentService
            .Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "TX123" });

        var orderService = new OrderService(mockPaymentService.Object);
        var order = new Order { Total = 100.00m, PaymentMethod = "Credit Card" };

        // Act
        var result = await orderService.ProcessAsync(order);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        mockPaymentService.Verify(
            p => p.ProcessPaymentAsync(100.00m, "Credit Card"),
            Times.Once);
    }

    [Test]
    public async Task ProcessOrder_HandlesPaymentFailure()
    {
        // Arrange
        var mockPaymentService = new Mock<IPaymentService>();
        mockPaymentService
            .Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ThrowsAsync(new PaymentException("Insufficient funds"));

        var orderService = new OrderService(mockPaymentService.Object);
        var order = new Order { Total = 1000.00m, PaymentMethod = "Credit Card" };

        // Act & Assert
        await Assert.That(async () => await orderService.ProcessAsync(order))
            .ThrowsExactly<PaymentException>()
            .WithMessage("Insufficient funds");
    }
}
```

### Using NSubstitute

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TUnit.Core;

public class OrderServiceNSubstituteTests
{
    [Test]
    public async Task ProcessOrder_CallsPaymentService()
    {
        // Arrange
        var paymentService = Substitute.For<IPaymentService>();
        paymentService
            .ProcessPaymentAsync(Arg.Any<decimal>(), Arg.Any<string>())
            .Returns(new PaymentResult { Success = true, TransactionId = "TX123" });

        var orderService = new OrderService(paymentService);
        var order = new Order { Total = 100.00m, PaymentMethod = "Credit Card" };

        // Act
        var result = await orderService.ProcessAsync(order);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await paymentService.Received(1).ProcessPaymentAsync(100.00m, "Credit Card");
    }

    [Test]
    public async Task ProcessOrder_HandlesPaymentFailure()
    {
        // Arrange
        var paymentService = Substitute.For<IPaymentService>();
        paymentService
            .ProcessPaymentAsync(Arg.Any<decimal>(), Arg.Any<string>())
            .Throws(new PaymentException("Insufficient funds"));

        var orderService = new OrderService(paymentService);
        var order = new Order { Total = 1000.00m, PaymentMethod = "Credit Card" };

        // Act & Assert
        await Assert.That(async () => await orderService.ProcessAsync(order))
            .ThrowsExactly<PaymentException>()
            .WithMessage("Insufficient funds");
    }
}
```

### Partial Mocks and Spy Pattern

```csharp
using Moq;

public class NotificationServiceTests
{
    [Test]
    public async Task SendNotification_LogsAttempt()
    {
        // Arrange - create a partial mock that calls real methods
        var mockLogger = new Mock<ILogger>();
        var notificationService = new Mock<NotificationService>(mockLogger.Object)
        {
            CallBase = true  // Call real implementation
        };

        // Override only the SendEmail method
        notificationService
            .Setup(n => n.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await notificationService.Object.NotifyUserAsync("user@example.com", "Hello");

        // Assert - verify the real method called SendEmail
        notificationService.Verify(
            n => n.SendEmailAsync("user@example.com", It.IsAny<string>()),
            Times.Once);
    }
}
```

## Data-Driven Test Patterns

### Using MethodDataSource

```csharp
using TUnit.Core;

public class CalculatorDataDrivenTests
{
    [Test]
    [MethodDataSource(nameof(GetCalculationTestCases))]
    public async Task Add_ReturnsCorrectSum(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Add(a, b);
        await Assert.That(result).IsEqualTo(expected);
    }

    public static IEnumerable<(int, int, int)> GetCalculationTestCases()
    {
        yield return (1, 2, 3);
        yield return (0, 0, 0);
        yield return (-1, 1, 0);
        yield return (100, 200, 300);
    }
}
```

### Using MethodDataSource with Complex Objects

```csharp
public class OrderValidationTests
{
    [Test]
    [MethodDataSource(nameof(GetInvalidOrders))]
    public async Task ValidateOrder_WithInvalidData_ReturnsErrors(
        Order order,
        string expectedError)
    {
        var validator = new OrderValidator();

        var result = await validator.ValidateAsync(order);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors).Contains(e => e.Message.Contains(expectedError));
    }

    public static IEnumerable<(Order, string)> GetInvalidOrders()
    {
        yield return (
            new Order { Total = 0, Items = new List<OrderItem>() },
            "Order must have at least one item"
        );

        yield return (
            new Order { Total = -10, Items = new List<OrderItem> { new() { Quantity = 1 } } },
            "Total must be positive"
        );

        yield return (
            new Order { Total = 100, CustomerId = 0, Items = new List<OrderItem> { new() } },
            "Customer ID is required"
        );
    }
}
```

### Using DataSourceGenerator

```csharp
using TUnit.Core;

public class UserTestDataGenerator : DataSourceGeneratorAttribute<User>
{
    public override IEnumerable<User> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return new User { Id = 1, Email = "user1@example.com", Role = "Admin" };
        yield return new User { Id = 2, Email = "user2@example.com", Role = "User" };
        yield return new User { Id = 3, Email = "user3@example.com", Role = "Guest" };
    }
}

public class UserPermissionTests
{
    [Test]
    [UserTestDataGenerator]
    public async Task CheckPermissions_ReturnsCorrectAccess(User user)
    {
        var permissionService = new PermissionService();

        var canDelete = await permissionService.CanDeleteAsync(user);

        if (user.Role == "Admin")
            await Assert.That(canDelete).IsTrue();
        else
            await Assert.That(canDelete).IsFalse();
    }
}
```

## Exception Testing

### Testing for Specific Exceptions

```csharp
using TUnit.Core;

public class ExceptionTests
{
    [Test]
    public async Task Divide_ByZero_ThrowsException()
    {
        var calculator = new Calculator();

        await Assert.That(() => calculator.Divide(10, 0))
            .ThrowsExactly<DivideByZeroException>();
    }

    [Test]
    public async Task CreateUser_WithInvalidEmail_ThrowsValidationException()
    {
        var userService = new UserService();

        await Assert.That(async () => await userService.CreateAsync("invalid-email", "John"))
            .ThrowsExactly<ValidationException>()
            .WithMessage("Invalid email format");
    }
}
```

### Testing Exception Messages and Properties

```csharp
public class DetailedExceptionTests
{
    [Test]
    public async Task ProcessPayment_InsufficientFunds_ThrowsWithDetails()
    {
        var paymentService = new PaymentService();
        var payment = new Payment { Amount = 1000, AccountBalance = 100 };

        var exception = await Assert.That(async () => await paymentService.ProcessAsync(payment))
            .ThrowsExactly<PaymentException>();

        await Assert.That(exception.Message).Contains("Insufficient funds");
        await Assert.That(exception.RequiredAmount).IsEqualTo(1000);
        await Assert.That(exception.AvailableAmount).IsEqualTo(100);
    }
}
```

### Testing Async Exceptions

```csharp
public class AsyncExceptionTests
{
    [Test]
    public async Task FetchData_WithInvalidUrl_ThrowsHttpException()
    {
        var apiClient = new ApiClient();

        // Test async method that throws
        await Assert.That(async () => await apiClient.GetDataAsync("invalid-url"))
            .ThrowsExactly<HttpRequestException>();
    }

    [Test]
    public async Task ProcessBatch_WithTimeout_ThrowsTaskCanceledException()
    {
        var processor = new BatchProcessor();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.That(async () => await processor.ProcessAsync(1000, cts.Token))
            .Throws<TaskCanceledException>();
    }
}
```

## Integration Test Patterns

### Testing with In-Memory Database

```csharp
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

public class OrderRepositoryIntegrationTests
{
    private DbContextOptions<OrderDbContext>? _options;
    private OrderDbContext? _context;

    [Before(Test)]
    public async Task Setup()
    {
        _options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(_options);
        await _context.Database.EnsureCreatedAsync();
    }

    [After(Test)]
    public async Task Cleanup()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    [Test]
    public async Task SaveOrder_PersistsToDatabase()
    {
        var repository = new OrderRepository(_context!);
        var order = new Order
        {
            CustomerId = 1,
            Total = 100.00m,
            Status = OrderStatus.Pending
        };

        await repository.SaveAsync(order);
        await _context!.SaveChangesAsync();

        var saved = await _context.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.Total).IsEqualTo(100.00m);
    }

    [Test]
    public async Task GetOrder_WithRelatedData_LoadsNavigationProperties()
    {
        // Seed data
        var order = new Order
        {
            CustomerId = 1,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, Quantity = 2, Price = 10.00m },
                new() { ProductId = 2, Quantity = 1, Price = 20.00m }
            }
        };
        _context!.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Test
        var repository = new OrderRepository(_context);
        var loaded = await repository.GetWithItemsAsync(order.Id);

        await Assert.That(loaded).IsNotNull();
        await Assert.That(loaded!.Items).HasCount().EqualTo(2);
    }
}
```

### Testing with Test Containers (Docker)

```csharp
using Testcontainers.PostgreSql;
using TUnit.Core;

public class PostgresIntegrationTests
{
    private PostgreSqlContainer? _container;
    private string? _connectionString;

    [Before(Test)]
    public async Task Setup()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("testdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
    }

    [After(Test)]
    public async Task Cleanup()
    {
        if (_container != null)
            await _container.DisposeAsync();
    }

    [Test]
    public async Task CanConnectToPostgres()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await Assert.That(connection.State).IsEqualTo(ConnectionState.Open);
    }
}
```

### Testing HTTP Client Integration

```csharp
using System.Net;
using TUnit.Core;

public class HttpClientIntegrationTests
{
    private HttpClient? _httpClient;

    [Before(Test)]
    public async Task Setup()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
    }

    [After(Test)]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }

    [Test]
    public async Task GetUser_ReturnsValidResponse()
    {
        var response = await _httpClient!.GetAsync("/users/1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<User>();
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.Id).IsEqualTo(1);
    }

    [Test]
    public async Task GetUser_HandlesRateLimiting()
    {
        // Make multiple requests to trigger rate limiting
        var tasks = Enumerable.Range(1, 100)
            .Select(i => _httpClient!.GetAsync($"/users/{i}"));

        var responses = await Task.WhenAll(tasks);

        var rateLimited = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        await Assert.That(rateLimited).IsGreaterThan(0);
    }
}
```

### Testing File System Operations

```csharp
using TUnit.Core;

public class FileServiceTests
{
    private string? _testDirectory;

    [Before(Test)]
    public async Task Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [After(Test)]
    public async Task Cleanup()
    {
        if (_testDirectory != null && Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Test]
    public async Task SaveFile_CreatesFileWithContent()
    {
        var fileService = new FileService();
        var fileName = "test.txt";
        var content = "Hello, World!";
        var filePath = Path.Combine(_testDirectory!, fileName);

        await fileService.SaveAsync(filePath, content);

        await Assert.That(File.Exists(filePath)).IsTrue();
        var savedContent = await File.ReadAllTextAsync(filePath);
        await Assert.That(savedContent).IsEqualTo(content);
    }

    [Test, NotInParallel("FileSystem")]
    public async Task DeleteFile_RemovesFile()
    {
        var filePath = Path.Combine(_testDirectory!, "delete-me.txt");
        await File.WriteAllTextAsync(filePath, "content");

        var fileService = new FileService();
        await fileService.DeleteAsync(filePath);

        await Assert.That(File.Exists(filePath)).IsFalse();
    }
}
```

## Summary

These cookbook recipes cover the most common testing scenarios. You can adapt these patterns for your specific needs:

- **Dependency Injection**: Use service collections for realistic testing with dependencies
- **API Testing**: Use `WebApplicationFactory` for end-to-end API tests
- **Mocking**: Choose Moq or NSubstitute based on your preference
- **Data-Driven Tests**: Use `MethodDataSource` or `DataSourceGenerator` for parameterized tests
- **Exception Testing**: Use TUnit's fluent exception assertions
- **Integration Tests**: Test with real databases, containers, or file systems

For more examples, check out the [examples directory](../examples/) in the documentation.
