---
sidebar_position: 5
---

# Advanced Features

## Events

### Raising Events

If the mocked interface declares events, TUnit.Mocks generates `Raise{EventName}()` extension methods directly on `Mock<T>`:

```csharp
public interface IConnection
{
    event EventHandler<string>? OnMessage;
    event Action? OnDisconnect;
}

var mock = Mock.Of<IConnection>();

// Subscribe to the event
string? received = null;
mock.Object.OnMessage += (sender, msg) => received = msg;

// Raise the event through the mock
mock.RaiseOnMessage("Hello!");
// received == "Hello!"
```

### Auto-Raise on Method Call

Trigger an event automatically when a method is called using the typed `.Raises{EventName}()` method on a setup chain:

```csharp
mock.SendMessage(Any<string>())
    .RaisesOnMessage("echo");

mock.Object.SendMessage("test");
// OnMessage event fires with "echo"
```

The typed raise methods are generated per-event with correct parameter types, giving you IntelliSense and compile-time safety. The string-based `.Raises(eventName, args)` overload is still available for dynamic scenarios.

### Event Subscription Tracking

Query and react to event subscriptions through the strongly-typed `Events` surface:

```csharp
var mock = Mock.Of<IConnection>();

// Register callbacks for subscribe/unsubscribe
mock.Events.OnMessage.OnSubscribe(() => Console.WriteLine("subscribed"));
mock.Events.OnMessage.OnUnsubscribe(() => Console.WriteLine("unsubscribed"));

mock.Object.OnMessage += (s, e) => { };
// prints "subscribed"

// Query subscriber info
var wasSubscribed = mock.Events.OnMessage.WasSubscribed;   // true
var count = mock.Events.OnMessage.SubscriberCount;          // 1
```

## State Machine Mocking

Model stateful behavior where method responses depend on the mock's current state:

```csharp
public interface IConnection
{
    string GetStatus();
    void Connect();
    void Disconnect();
}

var mock = Mock.Of<IConnection>();
Mock.SetState(mock, "disconnected");

Mock.InState(mock, "disconnected", m =>
{
    m.GetStatus().Returns("OFFLINE");
    m.Connect().TransitionsTo("connected");
});

Mock.InState(mock, "connected", m =>
{
    m.GetStatus().Returns("ONLINE");
    m.Disconnect().TransitionsTo("disconnected");
});

// Start disconnected
var status = mock.Object.GetStatus(); // "OFFLINE"

mock.Object.Connect(); // transitions to "connected"
status = mock.Object.GetStatus();     // "ONLINE"

mock.Object.Disconnect(); // transitions back to "disconnected"
status = mock.Object.GetStatus();     // "OFFLINE"
```

### State API

| Method | Description |
|---|---|
| `Mock.SetState(mock, "name")` | Set the current state |
| `Mock.SetState(mock, null)` | Clear state (all setups match) |
| `Mock.InState(mock, "name", configure)` | Register setups scoped to a state |
| `.TransitionsTo("name")` | Transition state after method call (on setup chain) |

## Recursive / Auto-Mocking

In loose mode, methods returning interface types automatically return functional mocks instead of null:

```csharp
public interface IServiceA
{
    IServiceB GetServiceB();
}

public interface IServiceB
{
    int GetValue();
}

var mock = Mock.Of<IServiceA>();

// GetServiceB() automatically returns an auto-mock
var serviceB = mock.Object.GetServiceB();
// serviceB is not null — it's a working mock

// Configure the auto-mock via Mock.Get
var autoMock = Mock.Get(serviceB);
autoMock.GetValue().Returns(42);

var value = serviceB.GetValue(); // 42
```

Use `Mock.Get(obj)` to retrieve the `Mock<T>` wrapper for any mock object — auto-mocked return values, or any object created by `Mock.Of`. Auto-mocks are cached — calling the same method returns the same mock instance.

## MockRepository

Manage multiple mocks with shared behavior and batch operations:

```csharp
var repo = new MockRepository(MockBehavior.Strict);

var serviceMock = repo.Of<IService>();
var loggerMock = repo.Of<ILogger>();

// Configure each mock individually
serviceMock.GetData(Any<int>()).Returns("result");
loggerMock.Log(Any<string>());

// Exercise code
serviceMock.Object.GetData(1);
loggerMock.Object.Log("hello");

// Batch verification
repo.VerifyAll();            // all setups invoked across all mocks
repo.VerifyNoOtherCalls();   // no unverified calls on any mock

// Batch reset
repo.Reset();                // clear all mocks
```

### Repository API

| Method | Description |
|---|---|
| `repo.Of<T>()` | Create and track a loose mock |
| `repo.Of<T>(behavior)` | Create and track a mock with specific behavior |
| `repo.OfPartial<T>(args)` | Create and track a partial mock |
| `repo.Track(existingMock)` | Add an existing mock to the repository |
| `repo.Mocks` | All tracked mocks |
| `repo.VerifyAll()` | Verify all setups on all mocks |
| `repo.VerifyNoOtherCalls()` | Verify no unverified calls on any mock |
| `repo.Reset()` | Reset all mocks |

## Diagnostics

Get a diagnostic report of setup coverage and call matching:

```csharp
mock.GetUser(Any<int>()).Returns(new User("Alice"));
mock.Delete(Any<int>());

svc.GetUser(1);
// Delete was never called

var diag = Mock.GetDiagnostics(mock);
diag.TotalSetups;       // 2
diag.ExercisedSetups;   // 1
diag.UnusedSetups;      // [Delete(Any<int>())]
diag.UnmatchedCalls;    // [] (all calls matched a setup)
```

Useful for debugging why a mock isn't behaving as expected, or for finding dead setups.

## Custom Default Value Provider

Override the default return values for unconfigured methods in loose mode:

```csharp
public class TestDefaults : IDefaultValueProvider
{
    public bool CanProvide(Type type)
        => type == typeof(string) || type == typeof(int);

    public object? GetDefaultValue(Type type) => type switch
    {
        _ when type == typeof(string) => "test-default",
        _ when type == typeof(int) => -1,
        _ => null
    };
}

var mock = Mock.Of<IService>();
Mock.SetDefaultValueProvider(mock, new TestDefaults());

var name = mock.Object.GetName();  // "test-default" (no setup needed)
var count = mock.Object.GetCount(); // -1
```

The provider is consulted **before** auto-mocking and built-in smart defaults.

## Reset

Clear all setups, call history, state, and auto-tracked property values:

```csharp
mock.GetUser(Any<int>()).Returns(new User("Alice"));
svc.GetUser(1);

Mock.Reset(mock);

svc.GetUser(1); // returns default (setup cleared)
Mock.GetInvocations(mock).Count; // 0 (history cleared)
```

The `SetupAllProperties()` flag is preserved across resets.
