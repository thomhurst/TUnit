# Quickstart: TUnit.Mocks

**Branch**: `001-mock-library` | **Date**: 2026-02-20

## Installation

```bash
dotnet add package TUnit.Mocks
```

That's it. The package includes the runtime library, source generator, and analyzers. No additional configuration needed.

## Your First Mock

```csharp
using TUnit.Mocks;

public interface IGreeter
{
    string Greet(string name);
}

[Test]
public void Should_Return_Configured_Greeting()
{
    // Arrange — create a mock
    var mock = Mock.Of<IGreeter>();

    // Configure — .Setup mirrors the interface
    mock.Setup.Greet("Alice").Returns("Hello, Alice!");

    // Act — pass mock directly (implicit conversion to IGreeter)
    IGreeter greeter = mock;
    var result = greeter.Greet("Alice");

    // Assert
    Assert.That(result).IsEqualTo("Hello, Alice!");
}
```

## Core Concepts

### 1. Create a Mock

```csharp
var mock = Mock.Of<IMyService>();          // loose mode (default)
var strict = Mock.Of<IMyService>(MockBehavior.Strict);  // strict mode
```

### 2. Configure Returns

```csharp
// Exact value match
mock.Setup.GetUser(42).Returns(new User("Alice"));

// Argument matchers
mock.Setup.GetUser(Arg.Any<int>()).Returns(new User("Default"));
mock.Setup.GetUser(Arg.Is<int>(id => id > 0)).Returns(new User("Found"));

// Async methods — just pass the unwrapped value
mock.Setup.GetUserAsync(42).Returns(new User("Alice"));  // auto-wraps in Task
```

### 3. Configure Exceptions

```csharp
mock.Setup.Delete(Arg.Any<int>()).Throws<UnauthorizedException>();
```

### 4. Configure Sequential Behaviors

```csharp
// Retry scenario: fail twice, then succeed
mock.Setup.Connect()
    .Throws<TimeoutException>()
    .Then().Throws<TimeoutException>()
    .Then().Returns(true);  // 3rd call and beyond returns true
```

### 5. Verify Calls

```csharp
mock.Verify.GetUser(42).WasCalled(Times.Once);
mock.Verify.Delete(Arg.Any<int>()).WasNeverCalled();
mock.Verify.Save(Arg.Any<User>()).WasCalled(Times.AtLeast(1));
```

### 6. Capture Arguments

```csharp
var captured = Arg.Capture<string>();
mock.Verify.SendEmail(captured, Arg.Any<string>()).WasCalled();

// Inspect what was passed
Assert.That(captured.Values[0]).IsEqualTo("alice@test.com");
```

### 7. Properties

```csharp
mock.Setup.Name.Returns("TestService");
mock.Verify.Name.GetWasCalled(Times.Once);
mock.Verify.Name.WasSetTo("Updated");
```

### 8. Events

```csharp
mock.Raise.OnStatusChanged("connected");
```

### 9. Reset

```csharp
mock.Reset();  // clears all setups and call history
```

### 10. VerifyAll and VerifyNoOtherCalls

```csharp
// VerifyAll — ensures every setup was actually called
mock.Setup.GetUser(Arg.Any<int>()).Returns(new User("Alice"));
// ... exercise code ...
mock.VerifyAll();  // fails if GetUser was never called

// VerifyNoOtherCalls — ensures no unexpected calls
mock.Verify!.GetUser(42).WasCalled(Times.Once);
mock.VerifyNoOtherCalls();  // fails if any other methods were called
```

### 11. Call Inspection

```csharp
// Access raw call history
foreach (var call in mock.Invocations)
{
    Console.WriteLine($"{call.MemberName}({string.Join(", ", call.Arguments)})");
}
```

### 12. Regex and Collection Matchers

```csharp
// Regex matcher
mock.Setup.Search(Arg.Matches(@"^test.*")).Returns(results);

// Collection matchers
mock.Setup.ProcessItems(Arg.Contains("important")).Returns(true);
mock.Setup.ProcessItems(Arg.HasCount<string>(3)).Returns(true);
mock.Setup.ProcessItems(Arg.IsEmpty<string>()).Returns(false);
```

### 13. Custom Argument Matchers

```csharp
public class EvenNumberMatcher : IArgumentMatcher<int>
{
    public bool Matches(int value) => value % 2 == 0;
    public bool Matches(object? value) => value is int i && Matches(i);
    public string Describe() => "an even number";
}

mock.Setup.Process(Arg.Matches(new EvenNumberMatcher())).Returns("even");
```

### 14. Multiple Interface Mocking

```csharp
var mock = Mock.Of<IService, IDisposable>();
mock.Setup.DoWork(Arg.Any<string>()).Returns(true);
// Can be cast to either interface
IService svc = mock;
IDisposable disp = (IDisposable)(object)mock.Object;
```

### 15. Mock Repository

```csharp
var repo = new MockRepository();
var mockA = repo.Of<IServiceA>();
var mockB = repo.Of<IServiceB>();

// ... exercise code ...

// Batch verify all mocks
repo.VerifyAll();
repo.VerifyNoOtherCalls();
repo.Reset();
```

### 16. Auto-Track Properties

```csharp
var mock = Mock.Of<IConfig>();
mock.SetupAllProperties();

IConfig config = mock;
config.Name = "test";         // auto-stored
var name = config.Name;       // returns "test"
```

## Complete Example

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task SaveAsync(Order order);
    event EventHandler<Order> OnOrderSaved;
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

[Test]
public async Task ProcessOrder_SendsConfirmationEmail()
{
    // Arrange
    var repo = Mock.Of<IOrderRepository>();
    var email = Mock.Of<IEmailService>();

    var order = new Order { Id = 1, CustomerEmail = "bob@test.com", Total = 99.99m };
    repo.Setup.GetByIdAsync(1).Returns(order);
    email.Setup.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);

    var sut = new OrderProcessor(repo, email);

    // Act
    await sut.ProcessAsync(1);

    // Assert — verify email was sent with correct recipient
    var capturedBody = Arg.Capture<string>();
    email.Verify.SendAsync(
        "bob@test.com",
        "Order Confirmation",
        capturedBody
    ).WasCalled(Times.Once);

    Assert.That(capturedBody.Latest).Contains("$99.99");

    // Verify order was saved
    repo.Verify.SaveAsync(Arg.Is<Order>(o => o.Status == OrderStatus.Confirmed))
        .WasCalled(Times.Once);
}
```

### 17. Strongly-Typed Callbacks (Source-Gen Exclusive)

```csharp
// Typed callback — compiler knows the parameter types
mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
    .Callback((int a, int b) => Console.WriteLine($"{a} + {b}"));

// Typed computed returns — no casting needed
mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
    .Returns((int a, int b) => a + b);

// Typed computed throws
mock.Setup.Divide(Arg.Any<int>(), Arg.Is(0))
    .Throws((int a, int b) => new DivideByZeroException($"Cannot divide {a} by {b}"));
```

### 18. Async Verification (TUnit Assertion Integration)

```csharp
// Integrates with Assert.That() — supports Assert.Multiple() and assertion scopes
await Assert.That(mock.Verify!.Add(1, 2)).WasCalled(Times.Once);
await Assert.That(mock.Verify!.Reset()).WasNeverCalled();

// Works inside Assert.Multiple:
await Assert.Multiple(() =>
{
    Assert.That(mock.Verify!.Save(Arg.Any<Data>())).WasCalled(Times.Exactly(3)),
    Assert.That(mock.Verify!.Delete(Arg.Any<int>())).WasNeverCalled()
});
```

### 19. State Machine Mocking

```csharp
var mock = Mock.Of<IConnection>();
mock.SetState("disconnected");

// Define behavior per state
mock.InState("disconnected", setup => {
    setup.Connect().TransitionsTo("connected");
    setup.GetStatus().Returns("OFFLINE");
    setup.Disconnect().Throws<InvalidOperationException>();
});

mock.InState("connected", setup => {
    setup.Connect().Throws<InvalidOperationException>();
    setup.GetStatus().Returns("ONLINE");
    setup.Disconnect().TransitionsTo("disconnected");
});

IConnection conn = mock;
await Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");
conn.Connect();                                          // → state: "connected"
await Assert.That(conn.GetStatus()).IsEqualTo("ONLINE");
conn.Disconnect();                                       // → back to "disconnected"
await Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");
```

### 20. Mock Diagnostics

```csharp
// After test execution, inspect mock health
var diag = mock.GetDiagnostics();

// Unused setups — setups you configured but never triggered (dead test code)
foreach (var unused in diag.UnusedSetups)
    Console.WriteLine($"Setup for {unused.MemberName} was never called");

// Unmatched calls — calls that hit no setup (potential missing coverage)
foreach (var unmatched in diag.UnmatchedCalls)
    Console.WriteLine($"Call to {unmatched.MemberName} had no matching setup");

// Coverage ratio
Console.WriteLine($"Setup coverage: {diag.ExercisedSetups}/{diag.TotalSetups}");
```

## Development Setup (Contributors)

### Build

```bash
cd TUnit.Mocks
dotnet build
```

### Run Tests

```bash
# Snapshot tests (source generator output)
dotnet test TUnit.Mocks.SourceGenerator.Tests

# Analyzer tests
dotnet test TUnit.Mocks.Analyzers.Tests

# Integration tests
dotnet test TUnit.Mocks.Tests
```

### Verify AOT Compatibility

```bash
dotnet publish TUnit.Mocks.Tests -p:PublishAot=true --use-current-runtime
```
