---
sidebar_label: NSubstitute
---

# Migrating from NSubstitute to TUnit.Mocks

TUnit.Mocks generates mocks at compile time instead of creating runtime proxies. The API is close to NSubstitute's: you call the member you want to configure and chain `.Returns()`. The main change is where the call happens. NSubstitute configures calls on the substitute itself. TUnit.Mocks configures them on a `Mock<T>` wrapper, which is also usable as the mocked interface.

TUnit.Mocks works with any test framework. You can migrate mocks before, after, or without migrating the tests to TUnit.

## Before You Start

1. Add the package:

   ```bash
   dotnet add package TUnit.Mocks
   ```

2. Set `<LangVersion>14</LangVersion>` (or `preview`) in the test project. TUnit.Mocks requires C# 14. Older versions report error `TM004`.
3. Read [Running Both Libraries Side by Side](#running-both-libraries-side-by-side) if you migrate one file at a time. TUnit.Mocks adds global usings, and its `Arg` type conflicts with `NSubstitute.Arg`.

## Quick Reference

| NSubstitute | TUnit.Mocks |
|---|---|
| `Substitute.For<IService>()` | `IService.Mock()` |
| `Substitute.For<IA, IB>()` | `Mock.Of<IA, IB>()` |
| `Substitute.ForPartsOf<MyClass>()` | `MyClass.Mock()` (unconfigured virtual members call the base implementation) |
| `Substitute.For<Func<int, string>>()` | `Mock.OfDelegate<Func<int, string>>()` |
| Substitute passed to the code under test | `mock` (converts implicitly) or `mock.Object` |
| `sub.Method(1).Returns(value)` | `mock.Method(1).Returns(value)` |
| `sub.Method(1).Returns(a, b, c)` | `mock.Method(1).ReturnsSequentially(a, b, c)` |
| `sub.Method(1).Returns(call => ...)` | `mock.Method(1).Returns((int id) => ...)` |
| `sub.MethodAsync(1).Returns(value)` | `mock.MethodAsync(1).Returns(value)` |
| `sub.Method(1).ReturnsForAnyArgs(value)` | `mock.Method(Any()).Returns(value)` (`Any()` for each parameter; see [Ignoring Arguments](#ignoring-arguments)) |
| `sub.Method(1).Throws(ex)` / `.ThrowsAsync(ex)` | `mock.Method(1).Throws(ex)` |
| `sub.When(x => x.Void()).Do(call => ...)` | `mock.Void().Callback(() => ...)` |
| `Arg.Any<T>()` | `Any()` or `Any<T>()` |
| `Arg.Is(value)` | `value` or `Is(value)` |
| `Arg.Is<T>(x => ...)` | `x => ...` or `Is<T>(x => ...)` |
| `Arg.Do<T>(x => list.Add(x))` | `var arg = Any<T>();` then read `arg.Values` |
| `sub.Received().Method(1)` | `mock.Method(1).WasCalled()` |
| `sub.Received(3).Method(1)` | `mock.Method(1).WasCalled(Times.Exactly(3))` |
| `sub.DidNotReceive().Method(1)` | `mock.Method(1).WasNeverCalled()` |
| `sub.ReceivedWithAnyArgs().Method(default)` | `mock.Method(Any()).WasCalled()` (`Any()` for each parameter; see [Ignoring Arguments](#ignoring-arguments)) |
| `Received.InOrder(() => { ... })` | `Mock.VerifyInOrder(() => { ... })` |
| `sub.ReceivedCalls()` | `mock.Invocations` |
| `sub.ClearReceivedCalls()` | `mock.Reset()` (also clears setups) |
| `sub.Event += Raise.EventWith(args)` | `mock.Raise{EventName}(args)` |
| Auto-properties on substitutes | `mock.SetupAllProperties()` |

<!-- doc-test-shared -->

## Example Types

The examples on this page use these types:

```csharp
public record User(int Id, string Name);

public sealed class UserSavedEventArgs(User user) : EventArgs
{
    public User User { get; } = user;
}

public interface IUserRepository
{
    User? GetById(int id);
    Task<User?> GetByIdAsync(int id);
    void Save(User user);
    bool TryGetName(int id, out string name);
    string ConnectionName { get; set; }
    event EventHandler<UserSavedEventArgs>? UserSaved;
}

public abstract class PriceCalculator
{
    public virtual decimal Tax(decimal amount) => amount * 0.2m;
    public abstract decimal Discount(decimal amount);
}
```

## Creating Mocks

NSubstitute returns the substitute as the interface type. TUnit.Mocks returns a `Mock<T>` wrapper. For interfaces, the wrapper also implements the interface, so you can pass it directly to the code under test. Use `.Object` when you need the `T` instance explicitly.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
IUserRepository sut = repository;
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
IUserRepository sut = repository;        // implicit conversion
IUserRepository same = repository.Object; // explicit instance
```

`T.Mock()` requires C# 14. You can also use the factory form `Mock.Of<IUserRepository>()`.

NSubstitute has no strict mode. TUnit.Mocks is loose by default, like NSubstitute. Pass `MockBehavior.Strict` to make unconfigured calls throw `MockStrictBehaviorException`:

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock(MockBehavior.Strict);
```

## Return Values

The basic setup is almost identical. The call goes to the mock wrapper instead of the substitute.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.GetById(1).Returns(new User(1, "Alice"));
repository.GetById(Arg.Any<int>()).Returns(new User(0, "Anyone"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(1).Returns(new User(1, "Alice"));
repository.GetById(Any()).Returns(new User(0, "Anyone"));
```

In both libraries, the most recently added matching setup wins.

### Computed Return Values

NSubstitute passes a `CallInfo` and you read arguments by type or position. TUnit.Mocks passes the method's arguments as typed lambda parameters.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.GetById(Arg.Any<int>()).Returns(call => new User(call.Arg<int>(), "Generated"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(Any()).Returns((int id) => new User(id, "Generated"));
```

### Sequential Return Values

NSubstitute's multi-value `Returns(a, b, c)` maps to `ReturnsSequentially`. In both libraries, the last value repeats after the sequence ends.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.GetById(1).Returns(new User(1, "First"), new User(1, "Second"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(1).ReturnsSequentially(new User(1, "First"), new User(1, "Second"));
```

To mix return values and exceptions across calls, chain `.Then()`:

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(1)
    .Throws<InvalidOperationException>()
    .Then()
    .Returns(new User(1, "Alice"));
```

### Async Methods

Both libraries accept the unwrapped value for `Task<T>` and `ValueTask<T>` members. TUnit.Mocks wraps it in the task for you, so async setups usually need no change.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.GetByIdAsync(1).Returns(new User(1, "Alice"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetByIdAsync(1).Returns(new User(1, "Alice"));
```

To return a task you create yourself, such as one from a `TaskCompletionSource`, use `ReturnsAsync(task)`.

### Ignoring Arguments

`ReturnsForAnyArgs` and `ReceivedWithAnyArgs` have no direct equivalent. Pass `Any()` for each parameter. Some methods also get an `AnyArgs()` shortcut that replaces all of them. It is not generated for overloaded or generic methods, for methods with fewer than two matchable parameters, or for methods with `out`, `ref`, or ref-struct parameters. See [when the shortcut is generated](../../writing-tests/mocking/argument-matchers.md#anyargs--match-every-parameter-with-one-token).

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.GetById(default).ReturnsForAnyArgs(new User(0, "Anyone"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(Any()).Returns(new User(0, "Anyone"));
```

## Exceptions

NSubstitute uses `Throws` and `ThrowsAsync` from `NSubstitute.ExceptionExtensions`. TUnit.Mocks uses `Throws` for both. For a method that returns `Task` or `ValueTask`, the mock returns a faulted task. It does not throw synchronously.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.GetById(-1).Throws(new ArgumentOutOfRangeException("id"));
repository.GetByIdAsync(-1).ThrowsAsync(new InvalidOperationException());
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(-1).Throws(new ArgumentOutOfRangeException("id"));
repository.GetByIdAsync(-1).Throws<InvalidOperationException>();
```

## Callbacks and Void Methods

NSubstitute configures void members with `When(...).Do(...)`. In TUnit.Mocks, call the void member on the mock and chain `Callback` or `Throws`. Typed callbacks receive the method's arguments.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
var saved = new List<User>();
repository.When(x => x.Save(Arg.Any<User>())).Do(call => saved.Add(call.Arg<User>()));
repository.When(x => x.Save(Arg.Is<User>(u => u.Id < 0))).Do(_ => throw new ArgumentException("Invalid id"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
var saved = new List<User>();
repository.Save(Any()).Callback((User user) => saved.Add(user));
repository.Save(user => user.Id < 0).Throws(new ArgumentException("Invalid id"));
```

`Callback` and `Throws` work on methods with return values too. For example, `.Callback(...).Returns(...)` runs the callback and returns the value.

## Argument Matchers

TUnit.Mocks imports its matchers globally, so you do not need the `Arg.` prefix. A raw value is an exact match, and a lambda is a predicate.

| NSubstitute | TUnit.Mocks |
|---|---|
| `Arg.Any<int>()` | `Any()` or `Any<int>()` |
| `Arg.Is(5)` | `5` or `Is(5)` |
| `Arg.Is<int>(x => x > 0)` | `x => x > 0` or `Is<int>(x => x > 0)` |
| `Arg.Is<string>(x => x == null)` | `IsNull<string>()` |
| `Arg.Is<string>(x => x != null)` | `IsNotNull<string>()` |
| `Arg.Is<string>(x => Regex.IsMatch(x, pattern))` | `Matches(pattern)` |
| `Arg.Do<T>(x => list.Add(x))` | Store an `Any<T>()` matcher and read `.Values` or `.Latest` |

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.GetById(Arg.Is<int>(id => id > 100)).Returns(new User(101, "Admin"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(id => id > 100).Returns(new User(101, "Admin"));
```

### Capturing Arguments

NSubstitute captures arguments with `Arg.Do`. In TUnit.Mocks, every matcher records the values it matches. Store the matcher in a variable and read it after the code under test runs.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
var saved = new List<User>();
repository.Save(Arg.Do<User>(user => saved.Add(user)));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
var userArg = Any<User>();
repository.Save(userArg);

repository.Object.Save(new User(1, "Alice"));

var saved = userArg.Values;   // every matched value
var last = userArg.Latest;    // the most recent value
```

See [Argument Matchers](../../writing-tests/mocking/argument-matchers.md) for collection, range, and custom matchers.

## Out and Ref Parameters

NSubstitute sets `out` values through the `CallInfo` argument array. TUnit.Mocks leaves `out` parameters out of the setup signature and generates a `SetsOut{ParameterName}` method for each one.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.TryGetName(1, out _).Returns(call =>
{
    call[1] = "Alice";
    return true;
});
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.TryGetName(1)
    .Returns(true)
    .SetsOutName("Alice");
```

`ref` parameters stay in the setup signature and use `SetsRef{ParameterName}`.

## Properties

NSubstitute substitutes behave like auto-properties: a value you set is returned by the getter. TUnit.Mocks properties return defaults until you configure them. Call `SetupAllProperties()` to get NSubstitute's behavior.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.ConnectionName.Returns("primary");

repository.ConnectionName = "replica"; // the getter now returns "replica"
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.ConnectionName.Returns("primary");

// Opt in to auto-property behavior
var tracked = IUserRepository.Mock();
tracked.SetupAllProperties();
tracked.Object.ConnectionName = "replica"; // the getter now returns "replica"
```

Setups made with `Returns` take precedence over values stored by `SetupAllProperties()`.

## Verifying Calls

NSubstitute checks calls with `Received()` before the member. TUnit.Mocks calls the member on the mock and then chains `WasCalled()` or `WasNeverCalled()`. Both throw when the check fails.

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();

repository.Received().Save(Arg.Is<User>(u => u.Name == "Alice"));
repository.Received(2).GetById(1);
repository.DidNotReceive().Save(Arg.Is<User>(u => u.Id < 0));
repository.ReceivedWithAnyArgs().GetById(default);
repository.Received().ConnectionName = "replica";
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();

repository.Save(user => user.Name == "Alice").WasCalled();
repository.GetById(1).WasCalled(Times.Exactly(2));
repository.Save(user => user.Id < 0).WasNeverCalled();
repository.GetById(Any()).WasCalled();
repository.ConnectionName.Set("replica").WasCalled();
```

`WasCalled()` without arguments means at least once, like `Received()`. The `Times` class also has `Once`, `Never`, `AtLeastOnce`, `AtLeast(n)`, `AtMost(n)`, and `Between(min, max)`.

In a TUnit test, you can also await the check as an assertion. This needs the separate `TUnit.Mocks.Assertions` package (`dotnet add package TUnit.Mocks.Assertions`) and `using TUnit.Mocks.Assertions;`:

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
await Assert.That(repository.GetById(1)).WasCalled(Times.Once);
```

### Call Order

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();

Received.InOrder(() =>
{
    repository.GetById(1);
    repository.Save(Arg.Any<User>());
});
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();

Mock.VerifyInOrder(() =>
{
    repository.GetById(1).WasCalled();
    repository.Save(Any()).WasCalled();
});
```

`Mock.VerifyInOrder` works across several mocks.

### Checks NSubstitute Does Not Have

TUnit.Mocks also provides `mock.VerifyAll()`, which fails if a setup was never used, and `mock.VerifyNoOtherCalls()`, which fails if a call was not verified. See [Verification](../../writing-tests/mocking/verification.md).

## Events

```csharp
// NSubstitute
var repository = Substitute.For<IUserRepository>();
repository.UserSaved += Raise.EventWith(new UserSavedEventArgs(new User(1, "Alice")));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.RaiseUserSaved(new UserSavedEventArgs(new User(1, "Alice")));
```

TUnit.Mocks generates a `Raise{EventName}` method for each event. To raise an event when a method is called, chain `.Raises{EventName}(...)` on a setup. See [Events](../../writing-tests/mocking/advanced.md#events).

## Partial Mocks and Classes

`Substitute.ForPartsOf<T>()` calls the real implementation for members you do not configure. A TUnit.Mocks class mock does the same for every unconfigured virtual member, so `T.Mock()` replaces `ForPartsOf<T>()`.

```csharp
// NSubstitute
var calculator = Substitute.ForPartsOf<PriceCalculator>();
calculator.Discount(Arg.Any<decimal>()).Returns(5m);
```

```csharp
// TUnit.Mocks
var calculator = PriceCalculator.Mock();
calculator.Discount(Any()).Returns(5m);

decimal tax = calculator.Object.Tax(100m); // base implementation: 20
```

This is different from `Substitute.For<SomeClass>()`, which does not call the base implementation of virtual members. If you depend on that, configure each member you call. Pass constructor arguments as typed parameters: `MyService.Mock("connection", 42)`.

TUnit.Mocks can also configure `protected` virtual and abstract members with the same syntax as public members. To wrap an existing instance of a non-sealed class and override only some of its virtual members, use `Mock.Wrap(instance)`. `Mock.Wrap` does not support interfaces.

## Other Features

| NSubstitute | TUnit.Mocks |
|---|---|
| Recursive mocks (interface members return substitutes) | Loose mocks return auto-mocks; use `Mock.Get(instance)` to configure one |
| `sub.ReceivedCalls()` | `mock.Invocations` (a list of `CallRecord`) |
| `sub.ClearReceivedCalls()` | `mock.Reset()`; it also clears setups and state |
| `Substitute.For<IA, IB>()` | `Mock.Of<IA, IB>()` (up to four types) |
| `.Configure()` before setting up a partial | Not needed; setup never calls the real member |

## Running Both Libraries Side by Side

TUnit.Mocks adds these global usings to the project:

- `TUnit.Mocks`
- `TUnit.Mocks.Arguments`
- `static TUnit.Mocks.Arguments.Arg`
- `TUnit.Mocks.Generated`

In a file that also has `using NSubstitute;`, the name `Arg` refers to two types. That causes error `CS0104`. If you migrate one file at a time, use one of these fixes:

- In files that still use NSubstitute, add `using Arg = NSubstitute.Arg;`.
- Or set `<TUnitMockImplicitUsings>disable</TUnitMockImplicitUsings>` in the project and add the four usings above to each migrated file.

When no file uses NSubstitute, remove the `NSubstitute` package reference. Also remove `NSubstitute.Analyzers` if the project uses it.

## Native AOT

NSubstitute creates proxies at runtime with `Reflection.Emit`, which Native AOT does not support. TUnit.Mocks generates mocks at compile time, so the migrated tests can run in a Native AOT or trimmed test application. See [AOT compatibility](../../writing-tests/aot.md).

## Next Steps

- [Setup and stubbing](../../writing-tests/mocking/setup.md)
- [Verification](../../writing-tests/mocking/verification.md)
- [Advanced features](../../writing-tests/mocking/advanced.md): state machines, auto-mocking, diagnostics, and `MockRepository`
