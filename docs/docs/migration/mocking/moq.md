---
sidebar_label: Moq
---

# Migrating from Moq to TUnit.Mocks

TUnit.Mocks generates mocks at compile time instead of creating runtime proxies. Moq configures a mock with an expression, such as `mock.Setup(x => x.GetById(1))`. TUnit.Mocks generates a strongly typed member on the mock for each member of the mocked type, so you call `mock.GetById(1)` directly. The chained method decides the meaning: `.Returns()` configures a setup, and `.WasCalled()` verifies calls.

TUnit.Mocks works with any test framework. You can migrate mocks before, after, or without migrating the tests to TUnit.

## Before You Start

1. Add the package:

   ```bash
   dotnet add package TUnit.Mocks
   ```

2. Set `<LangVersion>14</LangVersion>` (or `preview`) in the test project. TUnit.Mocks requires C# 14. Older versions report error `TM004`.
3. Read [Running Both Libraries Side by Side](#running-both-libraries-side-by-side) if you migrate one file at a time. `Mock<T>`, `Times`, `MockBehavior`, and `MockRepository` exist in both libraries.

## Quick Reference

| Moq | TUnit.Mocks |
|---|---|
| `new Mock<IService>()` | `IService.Mock()` |
| `new Mock<IService>(MockBehavior.Strict)` | `IService.Mock(MockBehavior.Strict)` |
| `new Mock<MyClass>(arg1, arg2) { CallBase = true }` | `MyClass.Mock(arg1, arg2)` |
| `mock.Object` | `mock.Object`, or `mock` itself (converts implicitly) |
| `mock.Setup(x => x.Method(1)).Returns(value)` | `mock.Method(1).Returns(value)` |
| `mock.Setup(x => x.MethodAsync(1)).ReturnsAsync(value)` | `mock.MethodAsync(1).Returns(value)` |
| `.Returns((int id) => ...)` | `.Returns((int id) => ...)` |
| `.Throws<T>()` / `.ThrowsAsync(ex)` | `.Throws<T>()` / `.Throws(ex)` |
| `.Callback<int>(id => ...)` | `.Callback((int id) => ...)` |
| `mock.SetupSequence(...)` | `.ReturnsSequentially(...)` or `.Then()` |
| `mock.SetupGet(x => x.Prop).Returns(value)` | `mock.Prop.Returns(value)` |
| `mock.SetupProperty(x => x.Prop)` / `mock.SetupAllProperties()` | `mock.SetupAllProperties()` |
| `It.IsAny<T>()` | `Any()` or `Any<T>()` |
| `It.Is<T>(x => ...)` | `x => ...` or `Is<T>(x => ...)` |
| `It.IsIn(...)` / `It.IsNotIn(...)` | `IsIn(...)` / `IsNotIn(...)` |
| `It.IsRegex(pattern)` | `Matches(pattern)` |
| `It.IsNotNull<T>()` | `IsNotNull<T>()` |
| `mock.Verify(x => x.Method(1), Times.Once())` | `mock.Method(1).WasCalled(Times.Once)` |
| `mock.Verify(x => x.Method(1), Times.Never())` | `mock.Method(1).WasNeverCalled()` |
| `mock.VerifySet(x => x.Prop = value)` | `mock.Prop.Set(value).WasCalled()` |
| `mock.Verify()` (`.Verifiable()` setups) | `.WasCalled()` on each of those calls |
| `mock.VerifyAll()` | `mock.VerifyAll()` (does not mark calls as verified for `VerifyNoOtherCalls()`) |
| `mock.VerifyNoOtherCalls()` | `mock.VerifyNoOtherCalls()` |
| `mock.Raise(x => x.Event += null, args)` | `mock.Raise{EventName}(args)` |
| `mock.As<IOther>()` | `Mock.Of<IService, IOther>()` |
| `Mock.Get(instance)` | `Mock.Get(instance)` |
| `mock.Invocations` | `mock.Invocations` |
| `mock.Reset()` | `mock.Reset()` |
| `new MockRepository(MockBehavior.Strict)` | `new MockRepository(MockBehavior.Strict)` |

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

Both libraries return a `Mock<T>` wrapper. For interfaces, the TUnit.Mocks wrapper also implements the interface, so you can pass the mock directly to the code under test. `.Object` still works.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
IUserRepository sut = repository.Object;
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
IUserRepository sut = repository;        // implicit conversion
IUserRepository same = repository.Object; // explicit instance
```

`T.Mock()` requires C# 14. You can also use the factory form `Mock.Of<IUserRepository>()`. Moq's `Mock.Of<T>()` returns the mocked object, but TUnit.Mocks' `Mock.Of<T>()` returns the `Mock<T>` wrapper.

Both libraries are loose by default. `MockBehavior.Strict` makes unconfigured calls throw. TUnit.Mocks throws `MockStrictBehaviorException`.

```csharp
// Moq
var repository = new Mock<IUserRepository>(MockBehavior.Strict);
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock(MockBehavior.Strict);
```

## Return Values

Remove the `Setup(x => ...)` expression and call the member on the mock.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.Setup(x => x.GetById(1)).Returns(new User(1, "Alice"));
repository.Setup(x => x.GetById(It.IsAny<int>())).Returns(new User(0, "Anyone"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(1).Returns(new User(1, "Alice"));
repository.GetById(Any()).Returns(new User(0, "Anyone"));
```

In both libraries, the most recently added matching setup wins.

### Computed Return Values

The lambda syntax is the same. The parameters are the method's arguments.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.Setup(x => x.GetById(It.IsAny<int>())).Returns((int id) => new User(id, "Generated"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(Any()).Returns((int id) => new User(id, "Generated"));
```

### Async Methods

Moq uses `ReturnsAsync`. In TUnit.Mocks, `Returns` wraps the value in `Task<T>` or `ValueTask<T>` for you.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User(1, "Alice"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetByIdAsync(1).Returns(new User(1, "Alice"));
```

TUnit.Mocks also has `ReturnsAsync`, but it takes a task, for example one from a `TaskCompletionSource`. Use it when the test controls when the task completes.

### Sequences

`SetupSequence` maps to `ReturnsSequentially` for values and to `.Then()` for mixed behavior.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.SetupSequence(x => x.GetById(1))
    .Throws<InvalidOperationException>()
    .Returns(new User(1, "First"))
    .Returns(new User(1, "Second"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(1)
    .Throws<InvalidOperationException>()
    .Then()
    .ReturnsSequentially(new User(1, "First"), new User(1, "Second"));
```

When a Moq sequence ends, later calls return the default value. In TUnit.Mocks, the last behavior repeats. In this example, every call after the third call returns `"Second"`.

Without `.Then()`, chained behaviors apply to the same call. For example, `.Returns(a).Returns(b)` always returns `b`.

## Exceptions

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.Setup(x => x.GetById(-1)).Throws(new ArgumentOutOfRangeException("id"));
repository.Setup(x => x.GetByIdAsync(-1)).ThrowsAsync(new InvalidOperationException());
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(-1).Throws(new ArgumentOutOfRangeException("id"));
repository.GetByIdAsync(-1).Throws<InvalidOperationException>();
```

TUnit.Mocks has no `ThrowsAsync`. For a method that returns `Task` or `ValueTask`, `Throws` returns a faulted task. It does not throw synchronously.

## Callbacks and Void Methods

Moq passes arguments to `Callback<T1, T2>(...)` through generic type arguments. TUnit.Mocks generates typed overloads, so declare the lambda parameter types.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
var saved = new List<User>();
repository.Setup(x => x.Save(It.IsAny<User>())).Callback<User>(user => saved.Add(user));
repository.Setup(x => x.Save(It.Is<User>(u => u.Id < 0))).Throws(new ArgumentException("Invalid id"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
var saved = new List<User>();
repository.Save(Any()).Callback((User user) => saved.Add(user));
repository.Save(user => user.Id < 0).Throws(new ArgumentException("Invalid id"));
```

In strict mode, a void setup with no chained behavior allows the call: `repository.Save(Any());`. It replaces `Setup(x => x.Save(It.IsAny<User>()))` with no `Callback`.

## Argument Matchers

TUnit.Mocks imports its matchers globally, so you do not need a prefix like `It.`. A raw value is an exact match, and a lambda is a predicate.

| Moq | TUnit.Mocks |
|---|---|
| `It.IsAny<int>()` | `Any()` or `Any<int>()` |
| `5` | `5` or `Is(5)` |
| `It.Is<int>(x => x > 0)` | `x => x > 0` or `Is<int>(x => x > 0)` |
| `It.IsIn(1, 2, 3)` | `IsIn(1, 2, 3)` |
| `It.IsNotIn(1, 2, 3)` | `IsNotIn(1, 2, 3)` |
| `It.IsInRange(1, 10, Range.Inclusive)` | `IsInRange(1, 10)` (always inclusive) |
| `It.IsRegex(pattern)` | `Matches(pattern)` |
| `It.IsNotNull<string>()` | `IsNotNull<string>()` |
| `It.Is<string>(x => x == null)` | `IsNull<string>()` |
| `It.Ref<int>.IsAny` | `Any()` |
| `Capture.In(list)` | Store an `Any<T>()` matcher and read `.Values` |

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.Setup(x => x.GetById(It.Is<int>(id => id > 100))).Returns(new User(101, "Admin"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(id => id > 100).Returns(new User(101, "Admin"));
```

### Capturing Arguments

Every TUnit.Mocks matcher records the values it matches. Store the matcher in a variable and read it after the code under test runs.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
var saved = new List<User>();
repository.Setup(x => x.Save(Capture.In(saved)));
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

Moq takes the `out` value from a variable in the setup expression. TUnit.Mocks leaves `out` parameters out of the setup signature and generates a `SetsOut{ParameterName}` method for each one.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
var name = "Alice";
repository.Setup(x => x.TryGetName(1, out name)).Returns(true);
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

TUnit.Mocks exposes each property on the mock. The getter is the default target, and `.Setter` or `.Set(value)` targets the setter.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.SetupGet(x => x.ConnectionName).Returns("primary");
repository.SetupSet(x => x.ConnectionName = "replica").Throws<InvalidOperationException>();
repository.SetupAllProperties();
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.ConnectionName.Returns("primary");
repository.ConnectionName.Set("replica").Throws<InvalidOperationException>();
repository.SetupAllProperties();
```

`SetupAllProperties()` makes every property store and return values. TUnit.Mocks has no per-property `SetupProperty`; use `SetupAllProperties()`, or configure the getter with `Returns`. Setups made with `Returns` take precedence over stored values.

## Verifying Calls

Remove the `Verify(x => ...)` expression, call the member on the mock, and chain `WasCalled` or `WasNeverCalled`. In TUnit.Mocks, `Times` members such as `Times.Once` are properties, not methods.

```csharp
// Moq
var repository = new Mock<IUserRepository>();

repository.Verify(x => x.Save(It.Is<User>(u => u.Name == "Alice")));
repository.Verify(x => x.GetById(1), Times.Exactly(2));
repository.Verify(x => x.Save(It.IsAny<User>()), Times.Never());
repository.VerifySet(x => x.ConnectionName = "replica", Times.Once());
repository.VerifyGet(x => x.ConnectionName, Times.AtLeastOnce());
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();

repository.Save(user => user.Name == "Alice").WasCalled();
repository.GetById(1).WasCalled(Times.Exactly(2));
repository.Save(Any()).WasNeverCalled();
repository.ConnectionName.Set("replica").WasCalled(Times.Once);
repository.ConnectionName.WasCalled(Times.AtLeastOnce);
```

| Moq | TUnit.Mocks |
|---|---|
| `Times.Once()` | `Times.Once` |
| `Times.Never()` | `Times.Never` or `.WasNeverCalled()` |
| `Times.AtLeastOnce()` | `Times.AtLeastOnce` or `.WasCalled()` |
| `Times.Exactly(n)` | `Times.Exactly(n)` |
| `Times.AtLeast(n)` / `Times.AtMost(n)` | `Times.AtLeast(n)` / `Times.AtMost(n)` |
| `Times.Between(min, max, Range.Inclusive)` | `Times.Between(min, max)` (always inclusive) |

Both libraries accept a failure message: `.WasCalled(Times.Once, "Save should run once")`.

In a TUnit test, you can also await the check as an assertion. This needs the separate `TUnit.Mocks.Assertions` package (`dotnet add package TUnit.Mocks.Assertions`) and `using TUnit.Mocks.Assertions;`:

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
await Assert.That(repository.GetById(1)).WasCalled(Times.Once);
```

### Verifiable Setups and VerifyAll

Moq's `mock.Verify()` checks only setups marked `.Verifiable()`. TUnit.Mocks has no `Verifiable()`, so verify each of those calls with `WasCalled()`. This also marks the call as verified for `VerifyNoOtherCalls()`. Do not replace `Verify()` with `VerifyAll()`: `VerifyAll()` also fails for unused setups that were not verifiable, and it does not mark calls as verified.

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.Setup(x => x.GetById(1)).Returns(new User(1, "Alice")).Verifiable();

repository.Object.GetById(1); // the code under test

repository.Verify();
repository.VerifyNoOtherCalls();
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(1).Returns(new User(1, "Alice"));

repository.Object.GetById(1); // the code under test

repository.GetById(1).WasCalled();
repository.VerifyNoOtherCalls();
```

### Call Order

Moq checks order with `MockSequence` and `InSequence`, which configure strict setups. TUnit.Mocks checks order after the code runs, across one or more mocks:

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();

Mock.VerifyInOrder(() =>
{
    repository.GetById(1).WasCalled();
    repository.Save(Any()).WasCalled();
});
```

## Events

```csharp
// Moq
var repository = new Mock<IUserRepository>();
repository.Raise(x => x.UserSaved += null, new UserSavedEventArgs(new User(1, "Alice")));
repository.Setup(x => x.Save(It.IsAny<User>()))
    .Raises(x => x.UserSaved += null, new UserSavedEventArgs(new User(2, "Bob")));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.RaiseUserSaved(new UserSavedEventArgs(new User(1, "Alice")));
repository.Save(Any())
    .RaisesUserSaved(new UserSavedEventArgs(new User(2, "Bob")));
```

TUnit.Mocks generates a `Raise{EventName}` method and a `.Raises{EventName}` setup method for each event. See [Events](../../writing-tests/mocking/advanced.md#events).

## Classes and Protected Members

Moq calls the base implementation only when `CallBase = true`. A TUnit.Mocks class mock always calls the base implementation for unconfigured virtual members. Pass constructor arguments as typed parameters.

```csharp
// Moq
var calculator = new Mock<PriceCalculator> { CallBase = true };
calculator.Setup(x => x.Discount(It.IsAny<decimal>())).Returns(5m);
```

```csharp
// TUnit.Mocks
var calculator = PriceCalculator.Mock();
calculator.Discount(Any()).Returns(5m);

decimal tax = calculator.Object.Tax(100m); // base implementation: 20
```

If a test depends on Moq's default `CallBase = false`, configure each virtual member that the test calls.

Moq configures `protected` members with `mock.Protected().Setup<T>("Name", ...)` and string names. TUnit.Mocks generates typed members for `protected` virtual and abstract members, so you configure and verify them like public members.

## Other Features

| Moq | TUnit.Mocks |
|---|---|
| `DefaultValue.Mock` | Default in loose mode: members that return interfaces return auto-mocks |
| `mock.DefaultValueProvider = ...` | `mock.DefaultValueProvider = ...` (implement `IDefaultValueProvider`) |
| `mock.As<IDisposable>()` | `Mock.Of<IUserRepository, IDisposable>()` (up to four types) |
| `Mock.Of<IService>(x => x.Prop == value)` | `IService.Mock()` and then `mock.Prop.Returns(value)` |
| `new Mock<Func<int, string>>()` | `Mock.OfDelegate<Func<int, string>>()` |
| `mock.Invocations.Clear()` | `mock.Reset()` (also clears setups and state) |
| `Mock.Get(instance)` | `Mock.Get(instance)` |

To wrap an existing instance of a non-sealed class and override only some of its virtual members, use `Mock.Wrap(instance)`. `Mock.Wrap` does not support interfaces. See [Advanced Features](../../writing-tests/mocking/advanced.md) for state machines, diagnostics, and `MockRepository`.

## Running Both Libraries Side by Side

TUnit.Mocks adds these global usings to the project:

- `TUnit.Mocks`
- `TUnit.Mocks.Arguments`
- `static TUnit.Mocks.Arguments.Arg`
- `TUnit.Mocks.Generated`

In a file that also has `using Moq;`, the names `Mock`, `Mock<T>`, `MockBehavior`, `MockRepository`, and `Times` each refer to two types. That causes error `CS0104`. If you migrate one file at a time, use one of these fixes:

- Set `<TUnitMockImplicitUsings>disable</TUnitMockImplicitUsings>` in the project and add the four usings above to each migrated file. Files that still use Moq then compile unchanged.
- Or, in files that still use Moq, move `using Moq;` inside the file's namespace declaration. A using directive inside a namespace takes precedence over global usings.

When no file uses Moq, remove the `Moq` package reference.

## Native AOT

Moq creates proxies at runtime with Castle DynamicProxy and `Reflection.Emit`, which Native AOT does not support. TUnit.Mocks generates mocks at compile time, so the migrated tests can run in a Native AOT or trimmed test application. See [AOT compatibility](../../writing-tests/aot.md).

## Next Steps

- [Setup and stubbing](../../writing-tests/mocking/setup.md)
- [Verification](../../writing-tests/mocking/verification.md)
- [Advanced features](../../writing-tests/mocking/advanced.md)
