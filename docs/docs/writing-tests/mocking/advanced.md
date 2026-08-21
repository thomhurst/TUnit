---
sidebar_position: 5
---

<!-- doc-test-ignore-file: Advanced examples depend on application interfaces and build on prior mock setup. -->

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

var mock = IConnection.Mock();

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
mock.SendMessage(Any())
    .RaisesOnMessage("echo");

mock.Object.SendMessage("test");
// OnMessage event fires with "echo"
```

The typed raise methods are generated per-event with correct parameter types, giving you IntelliSense and compile-time safety. The string-based `.Raises(eventName, args)` overload is still available for dynamic scenarios.

### Event Subscription Tracking

Query and react to event subscriptions through the strongly-typed `Events` surface:

```csharp
var mock = IConnection.Mock();

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

var mock = IConnection.Mock();
mock.SetState("disconnected");

mock.InState("disconnected", m =>
{
    m.GetStatus().Returns("OFFLINE");
    m.Connect().TransitionsTo("connected");
});

mock.InState("connected", m =>
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
| `mock.SetState("name")` | Set the current state |
| `mock.SetState(null)` | Clear state (all setups match) |
| `mock.InState("name", configure)` | Register setups scoped to a state |
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

var mock = IServiceA.Mock();

// GetServiceB() automatically returns an auto-mock
var serviceB = mock.Object.GetServiceB();
// serviceB is not null — it's a working mock

// Configure the auto-mock via Mock.Get
var autoMock = Mock.Get(serviceB);
autoMock.GetValue().Returns(42);

var value = serviceB.GetValue(); // 42
```

Use `Mock.Get(obj)` to retrieve the `Mock<T>` wrapper for any mock object — auto-mocked return values, or any object created by `T.Mock()`. Auto-mocks are cached — calling the same method returns the same mock instance.

### Runtime Auto-Stubs

Source-generated auto-mocks cover every interface the generator can name at compile time. Some
types it structurally cannot see — most commonly a generic method like `T Get<T>()` invoked
*inside* a third-party SDK with a `T` that is `internal` to that SDK, so your test assembly
cannot even write the type name:

```csharp
// Inside the Azure Functions Worker SDK — not your code:
//   features.Get<IFunctionBindingsFeature>()   // IFunctionBindingsFeature is internal to the SDK

var features = IInvocationFeatures.Mock();

// The SDK's internal Get<T>() call receives a functional runtime stub instead of null.
```

In loose mode, when no source-generated mock exists for a requested interface, TUnit.Mocks emits
a functional stub at runtime. Stubs are recursive and use the same defaults you'd expect from
runtime-proxy libraries like NSubstitute: strings return `""`, tasks come back completed,
collections are empty, value types are zeroed, and interface-returning members return further
stubs (or a real configurable `Mock<T>` when a source-generated factory exists for that type).
Properties round-trip values set on them, and member results are cached for stable identity.

The stub assembly is named `DynamicProxyGenAssembly2` and carries Castle DynamicProxy's public
key — the exact identity SDKs already grant `InternalsVisibleTo` for NSubstitute/Moq
compatibility — so any internal interface reachable by those libraries is reachable by
TUnit.Mocks stubs too.

Notes and limits:

- Runtime stubs are not configurable or verifiable — by definition you cannot name their types
  in test code. Nameable interfaces still get real, configurable mocks.
- Strict mode is unaffected: unconfigured calls still throw.
- On Native AOT (where `Reflection.Emit` does not exist) the feature is inert and unconfigured
  calls keep returning default values.
- The `netstandard2.0` asset (used by .NET Framework test projects) does not include the
  runtime emitter — unconfigured calls return default values there too. Runtime stubs require
  the `net8.0`+ assets.
- Opt out globally with `settings.Mocks.RuntimeAutoStubs = false;` in a
  `[Before(HookType.TestDiscovery)]` hook.

## MockRepository

Manage multiple mocks with shared behavior and batch operations:

```csharp
var repo = new MockRepository(MockBehavior.Strict);

var serviceMock = repo.Of<IService>();
var loggerMock = repo.Of<ILogger>();

// Configure each mock individually
serviceMock.GetData(Any()).Returns("result");
loggerMock.Log(Any());

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
| `repo.Of<T>(args)` | Create and track a mock with constructor args |
| `repo.Track(existingMock)` | Add an existing mock to the repository |
| `repo.Mocks` | All tracked mocks |
| `repo.VerifyAll()` | Verify all setups on all mocks |
| `repo.VerifyNoOtherCalls()` | Verify no unverified calls on any mock |
| `repo.Reset()` | Reset all mocks |

## Diagnostics

Get a diagnostic report of setup coverage and call matching:

```csharp
mock.GetUser(Any()).Returns(new User("Alice"));
mock.Delete(Any());

svc.GetUser(1);
// Delete was never called

var diag = mock.GetDiagnostics();
diag.TotalSetups;       // 2
diag.ExercisedSetups;   // 1
diag.UnusedSetups;      // [Delete(Any())]
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

var mock = IService.Mock();
mock.DefaultValueProvider = new TestDefaults();

var name = mock.Object.GetName();  // "test-default" (no setup needed)
var count = mock.Object.GetCount(); // -1
```

The provider is consulted **before** auto-mocking and built-in smart defaults.

## Reset

Clear all setups, call history, state, and auto-tracked property values:

```csharp
mock.GetUser(Any()).Returns(new User("Alice"));
svc.GetUser(1);

mock.Reset();

svc.GetUser(1); // returns default (setup cleared)
mock.Invocations.Count; // 0 (history cleared)
```

The `SetupAllProperties()` flag is preserved across resets.

## Internals Access (experimental)

Some SDKs route behavior through types that are `internal` to their own assembly — the classic
example is `Microsoft.Azure.Functions.Worker`, whose `IInvocationFeatures.Get<T>()` is called
inside the SDK with `T = IFunctionBindingsFeature`, a type your test assembly cannot even name.
Runtime-proxy libraries can auto-substitute such types (when the SDK grants `InternalsVisibleTo`
to Castle's proxy assembly), but they can never let you *configure* one.

TUnit.Mocks can, behind an experimental opt-in:

```xml
<PropertyGroup>
  <TUnitMocksExperimentalInternalsAccess>true</TUnitMocksExperimentalInternalsAccess>
</PropertyGroup>

<ItemGroup>
  <!-- Simple assembly name of any direct or transitive reference. -->
  <TUnitMocksInternalsAccess Include="Microsoft.Azure.Functions.Worker.Core" />
</ItemGroup>
```

Internal types of the listed assemblies then behave like public ones in your test project —
nameable, source-generator mocked, with fully typed setups, matchers, and verification. No
`InternalsVisibleTo` is required from the target assembly:

```csharp
var bindings = IFunctionBindingsFeature.Mock();          // internal to the SDK
bindings.InvocationResult.Returns(myResult);

features.Get<IFunctionBindingsFeature>().Returns(bindings.Object);
features.Get<IFunctionBindingsFeature>().WasCalled(Times.Once);
```

### How it works

At build time, each listed reference is swapped — for the compiler only — with a copy whose
internals are rewritten to public, preserving the assembly identity. The original assembly still
ships and loads; an `IgnoresAccessChecksTo` attribute (honored by the .NET runtime) makes the
compiled IL valid against it at execution time. This is the established "publicizer" pattern
used by several long-lived OSS tools, wired into the TUnit.Mocks package.

### Caveats

- **Experimental.** `IgnoresAccessChecksToAttribute` is honored by the runtime but is not a
  documented public contract.
- Not supported on .NET Framework test targets (the runtime there does not honor the attribute);
  a build warning is emitted and the pipeline stays inert.
- Works under trimmed publishes; Native AOT is not yet verified.
- If another package already injects an `IgnoresAccessChecksToAttribute` definition into your
  compilation (e.g. IgnoresAccessChecksToGenerator), suppress TUnit's copy with
  `<TUnitMocksInternalsAccessEmitAttributeDefinition>false</TUnitMocksInternalsAccessEmitAttributeDefinition>`.
- Internal APIs are internal for a reason: they can change in any release of the target package.
  Prefer public seams when they exist.
