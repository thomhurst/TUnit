---
sidebar_position: 1
---

# TUnit.Mocks

TUnit.Mocks is a **source-generated, AOT-compatible** mocking framework built for TUnit. Because mocks are generated at compile time, it works with Native AOT, trimming, and single-file publishing — unlike traditional mocking libraries that rely on runtime proxy generation.

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
        // Arrange — create a mock
        var mock = Mock.Of<IGreeter>();

        // Configure — set up a return value
        mock.Setup.Greet(Arg.Any<string>()).Returns("Hello!");

        // Act — use the mock object
        IGreeter greeter = mock.Object;
        var result = greeter.Greet("Alice");

        // Assert — verify the result and the call
        await Assert.That(result).IsEqualTo("Hello!");
        mock.Verify.Greet("Alice").WasCalled(Times.Once);
    }
}
```

## Key Concepts

### Creating Mocks

| Factory Method | Use Case |
|---|---|
| `Mock.Of<T>()` | Mock an interface or abstract class |
| `Mock.OfPartial<T>(args)` | Mock a concrete class (calls base for unconfigured methods) |
| `Mock.OfDelegate<T>()` | Mock a delegate (`Func<>`, `Action<>`, etc.) |
| `Mock.Wrap<T>(instance)` | Wrap a real object with selective overrides |
| `Mock.Of<T1, T2>()` | Mock multiple interfaces on a single object |
| `Mock.HttpHandler()` | Create a `MockHttpHandler` *(requires `TUnit.Mocks.Http`)* |
| `Mock.HttpClient(baseAddress?)` | Create a `MockHttpHandler` + `HttpClient` pair *(requires `TUnit.Mocks.Http`)* |
| `Mock.Logger()` | Create a `MockLogger` *(requires `TUnit.Mocks.Logging`)* |
| `Mock.Logger<T>()` | Create a `MockLogger<T>` implementing `ILogger<T>` *(requires `TUnit.Mocks.Logging`)* |

All factory methods accept an optional `MockBehavior` parameter:

```csharp
var loose = Mock.Of<IService>();                       // loose (default)
var strict = Mock.Of<IService>(MockBehavior.Strict);   // throws on unconfigured calls
```

### The Mock Wrapper

`Mock.Of<T>()` returns a `Mock<T>` wrapper with three surfaces:

```csharp
var mock = Mock.Of<IService>();

mock.Setup   // configure method/property behaviors
mock.Verify  // verify calls were made
mock.Raise   // raise events (if T has events)
mock.Object  // the T instance to pass to your code under test
```

### Implicit Conversion

You can pass `Mock<T>` directly where `T` is expected — no `.Object` needed:

```csharp
var mock = Mock.Of<IGreeter>();
IGreeter greeter = mock; // implicit conversion
```

### Loose vs Strict Mode

| Mode | Unconfigured methods | Default |
|---|---|---|
| `MockBehavior.Loose` | Return smart defaults (`0`, `""`, `false`, `null`, auto-mocked interfaces) | Yes |
| `MockBehavior.Strict` | Throw `MockStrictBehaviorException` | No |

## What's Next

- [Setup & Stubbing](setup) — configure return values, callbacks, exceptions, and property behaviors
- [Verification](verification) — verify calls, ordering, and assertion integration
- [Argument Matchers](argument-matchers) — match arguments with predicates, patterns, and capture values
- [Advanced Features](advanced) — state machines, events, auto-mocking, diagnostics, and more
- [HTTP Mocking](http) — mock `HttpClient` with `MockHttpHandler`
- [Logging](logging) — capture and verify `ILogger` calls with `MockLogger`
