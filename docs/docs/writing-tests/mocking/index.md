---
sidebar_position: 1
---

# TUnit.Mocks

TUnit.Mocks is a **standalone, source-generated, AOT-compatible** mocking framework. Because mocks are generated at compile time, it works with Native AOT, trimming, and single-file publishing — unlike traditional mocking libraries that rely on runtime proxy generation.

While it integrates seamlessly with TUnit's assertion engine, TUnit.Mocks has **no dependency on the TUnit test framework** and works with any test runner — xUnit, NUnit, MSTest, or no framework at all.

:::note Beta
TUnit.Mocks is currently in **beta**. The API may change before the stable release.
:::

## Installation

Add the NuGet package to your test project:

```bash
dotnet add package TUnit.Mocks --prerelease
```

For HTTP mocking or logging helpers, also add:

```bash
dotnet add package TUnit.Mocks.Http --prerelease
dotnet add package TUnit.Mocks.Logging --prerelease
```

:::warning C# 14 Required
TUnit.Mocks requires **C# 14** or later (`LangVersion` set to `14` or `preview`). If your project targets an older version, you will see error **TM004** at compile time.
:::

## Your First Mock

```csharp
using TUnit.Mocks;

public interface IGreeter
{
    string Greet(string name);
}

public class GreeterTests
{
    [Test]
    public async Task Greet_Returns_Configured_Value()
    {
        // Arrange — create a mock using the static extension syntax
        var mock = IGreeter.Mock();

        // Configure — set up a return value
        mock.Greet(Any()).Returns("Hello!");

        // Act — mock IS the interface, no .Object needed
        IGreeter greeter = mock;
        var result = greeter.Greet("Alice");

        // Assert — verify the result and the call
        await Assert.That(result).IsEqualTo("Hello!");
        mock.Greet("Alice").WasCalled(Times.Once);
    }
}
```

You can also use the `Mock.Of<T>()` factory — both produce the same result for interfaces:

```csharp
var mock = Mock.Of<IGreeter>();
```

## Key Concepts

### Creating Mocks

| Factory Method | Use Case |
|---|---|
| `Mock.Of<T>()` | Mock an interface, abstract class, or concrete class |
| `IMyInterface.Mock()` | Create a mock that directly implements the interface ([details](#typed-mock-wrapper)) |
| `Mock.OfDelegate<T>()` | Mock a delegate (`Func<>`, `Action<>`, etc.) |
| `Mock.Wrap<T>(instance)` | Wrap a real object with selective overrides |
| `Mock.Of<T1, T2>()` | Mock multiple interfaces on a single object |
| `[GenerateMock(typeof(T))]` | Generate a mock for interfaces with static abstract members ([details](setup#interfaces-with-static-abstract-members)) |
| `Mock.HttpHandler()` | Create a `MockHttpHandler` *(requires `TUnit.Mocks.Http`)* |
| `Mock.HttpClient(baseAddress?)` | Create a `MockHttpClient` — an `HttpClient` with a `.Handler` property *(requires `TUnit.Mocks.Http`)* |
| `Mock.Logger()` | Create a `MockLogger` *(requires `TUnit.Mocks.Logging`)* |
| `Mock.Logger<T>()` | Create a `MockLogger<T>` implementing `ILogger<T>` *(requires `TUnit.Mocks.Logging`)* |

All factory methods accept an optional `MockBehavior` parameter:

```csharp
var loose = Mock.Of<IService>();                       // loose (default)
var strict = Mock.Of<IService>(MockBehavior.Strict);   // throws on unconfigured calls
```

### The Mock Wrapper

`IService.Mock()` and `Mock.Of<T>()` return a `Mock<T>` wrapper (for interfaces, a generated subclass that also implements the interface). Extension methods are generated directly on `Mock<T>` for each member of the mocked type, and the chain methods (`.Returns()`, `.WasCalled()`, etc.) disambiguate between setup and verification:

```csharp
var mock = IService.Mock();

mock.GetUser(Any()).Returns(user);           // setup — .Returns() makes it a stub
mock.GetUser(42).WasCalled(Times.Once);      // verify — .WasCalled() makes it a check
mock.RaiseOnMessage("hi");                   // raise events — Raise{EventName}()
mock.Object                                  // the T instance (also available via direct cast)
```

### Typed Mock Wrapper

The `IMyInterface.Mock()` syntax (a C# 14 static extension member) returns a specialized wrapper type that extends `Mock<T>` **and** implements the interface directly. This means the mock can be used anywhere the interface is expected — no `.Object` or cast needed:

```csharp
var mock = IGreeter.Mock();

// mock IS an IGreeter — assign directly, pass to methods, use in collections
IGreeter greeter = mock;
List<IGreeter> greeters = [mock];
AcceptGreeter(mock);

// Setup and verification work the same way
mock.Greet(Any()).Returns("Hello!");
mock.Greet("Alice").WasCalled();
```

Both `Mock.Of<T>()` and `IMyInterface.Mock()` produce the same wrapper type for interfaces, so you can use them interchangeably. The `IMyInterface.Mock()` form is more concise and makes the intent clearer.

An optional `MockBehavior` parameter is supported:

```csharp
var strict = IGreeter.Mock(MockBehavior.Strict);
```

:::note
`IMyInterface.Mock()` requires C# 14 / .NET 10 or later (it uses C# 14 static extension members). For older language versions, or for multi-interface mocks, interfaces with static abstract members, delegates, partial mocks, and wrap mocks, use the `Mock.Of<T>()` / `Mock.Wrap()` / `Mock.OfDelegate<T>()` factory methods.
:::

### Implicit Conversion

`Mock<T>` also supports implicit conversion to `T` — so `Mock.Of<T>()` works without `.Object` too:

```csharp
var mock = Mock.Of<IGreeter>();
IGreeter greeter = mock; // implicit conversion
```

### Loose vs Strict Mode

| Mode | Unconfigured methods | Default |
|---|---|---|
| `MockBehavior.Loose` | Return smart defaults (`0`, `""`, `false`, `null`, auto-mocked interfaces) | Yes |
| `MockBehavior.Strict` | Throw `MockStrictBehaviorException` | No |

### Concise Argument Matching

TUnit.Mocks imports matchers globally — no `Arg.` prefix needed. Raw values, inline lambdas, and `Any()` work directly as arguments:

```csharp
var mock = Mock.Of<IUserService>();

// Any() — matches everything
mock.GetUser(Any()).Returns(user);

// Raw values — implicit exact matching
mock.GetUser(42).Returns(alice);

// Inline lambdas — predicate matching directly in the call
mock.GetUser(id => id > 0).Returns(validUser);
mock.GetByRole(role => role == "admin").Returns(admins);

// Mix lambdas with Any() or raw values
mock.Search(name => name.StartsWith("A"), Any()).Returns(results);

// Is<T>() — explicit predicate matching (also works)
mock.GetUser(Is<int>(id => id > 0)).Returns(validUser);
```

See [Argument Matchers](argument-matchers) for the full API.

## What's Next

- [Setup & Stubbing](setup) — configure return values, callbacks, exceptions, and property behaviors
- [Verification](verification) — verify calls, ordering, and assertion integration
- [Argument Matchers](argument-matchers) — match arguments with predicates, patterns, and capture values
- [Advanced Features](advanced) — state machines, events, auto-mocking, diagnostics, and more
- [HTTP Mocking](http) — mock `HttpClient` with `MockHttpHandler`
- [Logging](logging) — capture and verify `ILogger` calls with `MockLogger`
