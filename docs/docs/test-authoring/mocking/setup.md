---
sidebar_position: 2
---

# Setup & Stubbing

Methods are called directly on `Mock<T>` — the chain method (`.Returns()`, `.Throws()`, etc.) makes it a setup.

## Method Setup

### Return Values

```csharp
// Fixed return value
mock.GetUser(Arg.Any<int>()).Returns(new User("Alice"));

// Computed return value
mock.GetUser(Arg.Any<int>()).Returns(() => new User(DateTime.Now.ToString()));

// Async methods — no special API needed
mock.GetUserAsync(Arg.Any<int>()).Returns(new User("Alice"));
// TUnit.Mocks auto-wraps the value in Task<T> or ValueTask<T>
```

### Throwing Exceptions

```csharp
// Throw a specific exception type
mock.Delete(Arg.Any<int>()).Throws<InvalidOperationException>();

// Throw a specific instance
mock.Delete(Arg.Any<int>()).Throws(new ArgumentException("bad id"));
```

### Callbacks

```csharp
// Simple callback
var callCount = 0;
mock.Process(Arg.Any<string>())
    .Callback(() => callCount++);

// Callback with access to arguments
mock.Process(Arg.Any<string>())
    .Callback((object?[] args) => Console.WriteLine($"Called with: {args[0]}"));
```

### Sequential Behaviors

Use `.Then()` to define different behaviors for successive calls:

```csharp
mock.GetValue(Arg.Any<string>())
    .Throws<InvalidOperationException>()   // 1st call: throws
    .Then()
    .Returns("retry-succeeded")             // 2nd call: returns value
    .Then()
    .Returns("cached");                     // 3rd+ calls: returns this value

// Shorthand for sequential return values
mock.GetValue(Arg.Any<string>())
    .ReturnsSequentially("first", "second", "third");
// 1st: "first", 2nd: "second", 3rd+: "third" (last value repeats)
```

### Void Methods

Void methods support `Callback` and `Throws` (but not `Returns`):

```csharp
mock.Log(Arg.Any<string>())
    .Callback(() => { /* side effect */ });

mock.Log(Arg.Any<string>())
    .Throws<NotSupportedException>();
```

Void methods are also eagerly registered — calling `mock.Log(Arg.Any<string>())` without chaining is sufficient to "allow" the call in strict mode.

## Property Setup

TUnit.Mocks uses C# 14 extension properties for a natural property API. The default behavior targets the **getter** (the most common case).

### Getter Setup

```csharp
// These are equivalent — both configure the getter
mock.Name.Returns("Alice");
mock.Name.Getter.Returns("Alice");
```

All method setup operations work on getters:

```csharp
mock.Name.Throws<InvalidOperationException>();
mock.Name.Callback(() => Console.WriteLine("Name accessed"));
mock.Name.ReturnsSequentially("first", "second");
```

### Setter Setup

```csharp
// React to any value being set
mock.Count.Setter.Callback(() => Console.WriteLine("Count was set"));

// React to a specific value being set
mock.Count.Set(Arg.Is(42)).Callback(() => Console.WriteLine("Count set to 42"));

// Throw on setter
mock.Name.Setter.Throws<NotSupportedException>();
```

### Auto-Tracking Properties

Call `SetupAllProperties()` to make properties behave like real auto-properties — setters store values, getters return them:

```csharp
var mock = Mock.Of<IEntity>();
Mock.SetupAllProperties(mock);

mock.Object.Name = "Alice";
var name = mock.Object.Name; // "Alice"

mock.Object.Count = 10;
var count = mock.Object.Count; // 10
```

Explicit setups take precedence over auto-tracked values.

## Out and Ref Parameters

**Out parameters** are excluded from setup signatures. Use the generated strongly-typed `.SetsOut{Name}()` methods to assign their values:

```csharp
// Strongly-typed — named after the parameter, compile-time safe
mock.TryGet("key")
    .Returns(true)
    .SetsOutValue("found-value");

bool found = svc.TryGet("key", out var value);
// found == true, value == "found-value"
```

**Ref parameters** are included in setup signatures and participate in argument matching. Use `.SetsRef{Name}()` to assign output values:

```csharp
mock.Swap(Arg.Any<int>())
    .SetsRefValue(99);

int val = 42;
svc.Swap(ref val);
// val == 99
```

The method names are derived from the original parameter names — e.g. `out string value` produces `.SetsOutValue()`, `ref int count` produces `.SetsRefCount()`. This gives you IntelliSense discoverability and compile-time type safety.

The untyped `.SetsOutParameter(index, value)` overload remains available for advanced scenarios but is hidden from IntelliSense on typed wrappers.

## Partial Mocks

Partial mocks wrap a real class. Unconfigured method calls execute the base implementation:

```csharp
public abstract class Calculator
{
    public virtual int Add(int a, int b) => a + b;
    public abstract int Multiply(int a, int b);
}

var mock = Mock.OfPartial<Calculator>();
mock.Multiply(Arg.Any<int>(), Arg.Any<int>()).Returns(99);

mock.Object.Add(2, 3);      // 5 (base implementation)
mock.Object.Multiply(2, 3); // 99 (mocked)
```

Pass constructor arguments for non-default constructors:

```csharp
var mock = Mock.OfPartial<MyService>("connectionString", 42);
```

## Delegate Mocking

Mock any delegate type:

```csharp
var mock = Mock.OfDelegate<Func<string, int>>();
mock.Invoke(Arg.Any<string>()).Returns(42);

Func<string, int> func = mock;
var result = func("hello"); // 42
```

Works with `Action<>`, `Func<>`, and custom delegate types.

## Wrapping Real Objects

Wrap a real instance to selectively override methods while delegating unconfigured calls to the real implementation:

```csharp
var realService = new ProductionService();
var mock = Mock.Wrap(realService);

// Override just one method
mock.GetConfig().Returns(new TestConfig());

// All other calls go to realService
mock.Object.DoWork(); // calls realService.DoWork()
```

## Multi-Interface Mocks

Create a single mock that implements multiple interfaces:

```csharp
var mock = Mock.Of<ILogger, IDisposable>();

mock.Log(Arg.Any<string>()); // ILogger method
mock.Object.Log("test");

((IDisposable)mock.Object).Dispose(); // IDisposable method
```

Supports up to 4 interfaces: `Mock.Of<T1, T2, T3, T4>()`.

## Setup Chaining

Setup methods return chain objects that support additional behaviors:

```csharp
mock.Process(Arg.Any<int>())
    .Returns(true)
    .RaisesProcessCompleted(EventArgs.Empty)   // strongly-typed auto-raise event
    .TransitionsTo("processed");               // state machine transition
```

The typed `.Raises{EventName}()` methods provide IntelliSense and compile-time safety for event parameters. The string-based `.Raises(eventName, args)` overload is also available.

See [Advanced Features](advanced) for details on events and state machines.
