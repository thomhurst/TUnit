---
sidebar_position: 1
---


# TUnit.Mocks

TUnit.Mocks is a **source-generated, AOT-compatible** mocking framework. Because mocks are generated at compile time, it works with Native AOT, trimming, and single-file publishing — unlike traditional mocking libraries that rely on runtime proxy generation.

While it integrates seamlessly with TUnit's assertion engine, TUnit.Mocks does **not require the TUnit test runner** and works with any test runner — xUnit, NUnit, MSTest, or no framework at all.

## Installation

Add the NuGet package to your test project:

```bash
dotnet add package TUnit.Mocks
```

For HTTP mocking or logging helpers, also add:

```bash
dotnet add package TUnit.Mocks.Http
dotnet add package TUnit.Mocks.Logging
```

:::warning C# 14 Required
TUnit.Mocks requires **C# 14** or later (`LangVersion` set to `14` or `preview`). If your project targets an older version, you will see error **TM004** at compile time.
:::

:::note Generated namespaces
The setup, verification and event surface is generated into `TUnit.Mocks.Generated`, which TUnit.Mocks adds as a global using. It does not matter which namespace the mocked type itself lives in. If you set `<TUnitMockImplicitUsings>disable</TUnitMockImplicitUsings>`, add `using TUnit.Mocks.Generated;` yourself or the setup methods will not be found.
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

Prefer `T.Mock()` for creating mocks. The `Mock.Of<T>()` factory is equivalent, but reach for it only where `T.Mock()` is not available, such as multi-interface mocks:

```csharp
var mock = Mock.Of<IGreeter>(); // equivalent to IGreeter.Mock(), which is preferred
```

The **TM010** suggestion flags `Mock.Of<T>()` and `Mock.Of<T>(behavior)` calls that can use `T.Mock()` instead, and its code fix rewrites them (adding `using TUnit.Mocks;` when needed). Use "Fix all" in your IDE, or run `dotnet format analyzers --diagnostics TM010 --severity info` to migrate a whole project.

## Key Concepts

### Creating Mocks

| Factory Method | Use Case |
|---|---|
| `T.Mock()` | Mock an interface, abstract class, or concrete class — the recommended syntax ([details](#typed-mock-wrapper)) |
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
var loose = IService.Mock();                           // loose (default)
var strict = IService.Mock(MockBehavior.Strict);       // throws on unconfigured calls
```

### Global Default Mode

In TUnit test projects, you can change the default mode for all mocks that are created without an explicit `MockBehavior`:

```csharp
using TUnit.Core;
using TUnit.Mocks;

public class GlobalSetup
{
    [Before(HookType.TestDiscovery)]
    public static void Configure(BeforeTestDiscoveryContext context)
    {
        context.Settings.Mocks.DefaultMode = MockBehavior.Strict;
    }
}
```

With this setting, `IService.Mock()`, `Mock.Of<IService>()`, `Mock.Wrap(instance)`, `Mock.OfDelegate<T>()`, and `new MockRepository()` use strict mode by default. Passing a `MockBehavior` still overrides the global default:

```csharp
var strict = IService.Mock();                    // uses global strict default
var loose = IService.Mock(MockBehavior.Loose);   // explicit override
```

### The Mock Wrapper

`T.Mock()` returns a `Mock<T>` wrapper (for interfaces, a generated subclass that also implements the interface). Extension methods are generated directly on `Mock<T>` for each member of the mocked type, and the chain methods (`.Returns()`, `.WasCalled()`, etc.) disambiguate between setup and verification:

```csharp
var mock = IService.Mock();

mock.GetUser(Any()).Returns(user);           // setup — .Returns() makes it a stub
mock.GetUser(42).WasCalled(Times.Once);      // verify — .WasCalled() makes it a check
mock.RaiseOnMessage(mock.Object, "hi");      // raise events — Raise{EventName}()
_ = mock.Object;                            // the T instance (also available via direct cast)
```

### Typed Mock Wrapper

For interfaces, `IMyInterface.Mock()` (a C# 14 static extension member) returns a specialized wrapper type that extends `Mock<T>` **and** implements the interface directly. This means the mock can be used anywhere the interface is expected — no `.Object` or cast needed:

```csharp
var mock = IGreeter.Mock();
static void AcceptGreeter(IGreeter greeter) { }

// mock IS an IGreeter — assign directly, pass to methods, use in collections
IGreeter greeter = mock;
List<IGreeter> greeters = [mock];
AcceptGreeter(mock);

// Setup and verification work the same way
mock.Greet(Any()).Returns("Hello!");
mock.Greet("Alice").WasCalled();
```

The wrapper type is named after the interface with a `Mock` suffix (`IGreeter` → `IGreeterMock`) and lives in the interface's namespace. Declaring a variable, field or property as `Mock<IGreeter>` hides the wrapper. C# never applies user-defined conversions to interface types, so a `Mock<IGreeter>` needs `.Object` wherever an `IGreeter` is expected, and generic type inference, casts and `is`/`as` checks see `Mock<IGreeter>` rather than an `IGreeter`. The **TM011** suggestion flags locals, private fields and private properties declared as `Mock<T>` that only ever hold the wrapper, and its code fix changes the declaration to the wrapper type:

```csharp
Mock<IGreeter> before = IGreeter.Mock(); // TM011: declare 'before' as 'IGreeterMock'
IGreeterMock after = IGreeter.Mock();    // after the fix
```

Run `dotnet format analyzers --diagnostics TM011 --severity info` to apply it to a whole project.

`T.Mock()` is the recommended syntax for all types — interfaces, abstract classes, and concrete classes. For interfaces it returns a typed wrapper; for classes it returns `Mock<T>`. Constructor arguments are supported as strongly-typed parameters:

```csharp
var strict = IGreeter.Mock(MockBehavior.Strict);
var service = MyService.Mock("connectionString", 42);
```

:::note
TUnit.Mocks requires C# 14 or later (`LangVersion` 14 or `preview`), because its generated code uses C# 14 extension members. This applies to every creation syntax, including `Mock.Of<T>()`; older language versions report **TM004**. For multi-interface mocks, delegates, and wrap mocks, use the `Mock.Of<T1, T2>()`, `Mock.OfDelegate<T>()`, and `Mock.Wrap()` factory methods. For interfaces with static abstract members, see [`[GenerateMock]`](setup#interfaces-with-static-abstract-members).
:::

### Implicit Conversion

`Mock<T>` declares an implicit conversion to `T`, so a class mock works without `.Object`. C# never applies user-defined conversions to interface types, so for an interface mock either keep the wrapper type that `T.Mock()` returns, which implements the interface, or use `.Object`:

```csharp
var mock = IGreeter.Mock();
IGreeter greeter = mock; // the wrapper implements IGreeter

Mock<IGreeter> baseMock = mock;
IGreeter viaObject = baseMock.Object; // Mock<IGreeter> needs .Object
```

### Loose vs Strict Mode

| Mode | Unconfigured methods | Default |
|---|---|---|
| `MockBehavior.Loose` | Return smart defaults (`0`, `""`, `false`, `null`, auto-mocked interfaces) | Yes |
| `MockBehavior.Strict` | Throw `MockStrictBehaviorException` | No |

### Concise Argument Matching

TUnit.Mocks imports matchers globally — no `Arg.` prefix needed. Raw values, inline lambdas, and `Any()` work directly as arguments:

```csharp
var mock = IUserService.Mock();

// Any() — matches everything
mock.GetUser(Any()).Returns(user);

// Raw values — implicit exact matching
mock.GetUser(42).Returns(alice);

// Inline lambdas — predicate matching directly in the call
mock.GetUser(id => id > 0).Returns(validUser);
mock.GetByRole(role => role == "admin").Returns(users);

// Mix lambdas with Any() or raw values
mock.Search(name => name.StartsWith("A"), Any()).Returns(users);

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
- Migrating from another library — [Moq](../../migration/mocking/moq.md), [NSubstitute](../../migration/mocking/nsubstitute.md), or [FakeItEasy](../../migration/mocking/fakeiteasy.md)
