---
sidebar_label: FakeItEasy
---

# Migrating from FakeItEasy to TUnit.Mocks

TUnit.Mocks generates mocks at compile time instead of creating runtime proxies. FakeItEasy configures a fake through `A.CallTo(() => fake.Member(...))`. TUnit.Mocks generates a strongly typed member on the mock for each member of the mocked type, so you call `mock.Member(...)` directly. The chained method decides the meaning: `.Returns()` configures a setup, and `.WasCalled()` verifies calls.

TUnit.Mocks works with any test framework. You can migrate mocks before, after, or without migrating the tests to TUnit.

## Before You Start

1. Add the package:

   ```bash
   dotnet add package TUnit.Mocks
   ```

2. Set `<LangVersion>14</LangVersion>` (or `preview`) in the test project. TUnit.Mocks requires C# 14. Older versions report error `TM004`.
3. Read [Running Both Libraries Side by Side](#running-both-libraries-side-by-side) if you migrate one file at a time. `Times` exists in both libraries.

## Quick Reference

| FakeItEasy | TUnit.Mocks |
|---|---|
| `A.Fake<IService>()` | `IService.Mock()` |
| `A.Fake<IService>(o => o.Strict())` | `IService.Mock(MockBehavior.Strict)` |
| `A.Fake<MyClass>(o => o.WithArgumentsForConstructor(...))` | `MyClass.Mock(arg1, arg2)` |
| `A.Fake<IService>(o => o.Implements<IOther>())` | `Mock.Of<IService, IOther>()` |
| `A.Fake<MyClass>(o => o.Wrapping(real))` | `Mock.Wrap(real)` (non-sealed classes only; no equivalent for interfaces) |
| The fake passed to the code under test | `mock` (converts implicitly) or `mock.Object` |
| `A.CallTo(() => fake.Method(1)).Returns(value)` | `mock.Method(1).Returns(value)` |
| `.ReturnsLazily((int id) => ...)` | `.Returns((int id) => ...)` |
| `.ReturnsNextFromSequence(a, b)` | `.ReturnsSequentially(a, b)` |
| `.Returns(a).Once().Then.Returns(b)` | `.Returns(a).Then().Returns(b)` |
| `.Throws<T>()` / `.ThrowsAsync(ex)` | `.Throws<T>()` / `.Throws(ex)` |
| `.Invokes((int id) => ...)` | `.Callback((int id) => ...)` |
| `.DoesNothing()` | `mock.VoidMethod(...)` with no chained behavior |
| `.CallsBaseMethod()` | Default for class mocks |
| `.AssignsOutAndRefParameters(value)` | `.SetsOut{ParameterName}(value)` |
| `A<T>.Ignored` / `A<T>._` | `Any()` or `Any<T>()` |
| `A<T>.That.Matches(x => ...)` | `x => ...` or `Is<T>(x => ...)` |
| `.WithAnyArguments()` | `Any()` for each parameter (see [Argument Constraints](#argument-constraints)) |
| `.MustHaveHappened()` | `.WasCalled()` |
| `.MustHaveHappenedOnceExactly()` | `.WasCalled(Times.Once)` |
| `.MustNotHaveHappened()` | `.WasNeverCalled()` |
| `A.CallToSet(() => fake.Prop).To(value)` | `mock.Prop.Set(value)` |
| `fake.Event += Raise.With(args)` | `mock.Raise{EventName}(args)` |
| `Fake.GetCalls(fake)` | `mock.Invocations` |
| `Fake.ClearRecordedCalls(fake)` | `mock.Reset()` (also clears setups) |

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

FakeItEasy returns the fake as the interface type. TUnit.Mocks returns a `Mock<T>` wrapper. For interfaces, the wrapper also implements the interface, so you can pass it directly to the code under test. Use `.Object` when you need the `T` instance explicitly.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
IUserRepository sut = repository;
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
IUserRepository sut = repository;        // implicit conversion
IUserRepository same = repository.Object; // explicit instance
```

`T.Mock()` requires C# 14. You can also use the factory form `Mock.Of<IUserRepository>()`.

Both libraries are loose by default. In TUnit.Mocks, a strict mock throws `MockStrictBehaviorException` for unconfigured calls.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>(options => options.Strict());
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock(MockBehavior.Strict);
```

## Return Values

Remove `A.CallTo(() => ...)` and call the member on the mock.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
A.CallTo(() => repository.GetById(1)).Returns(new User(1, "Alice"));
A.CallTo(() => repository.GetById(A<int>.Ignored)).Returns(new User(0, "Anyone"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(1).Returns(new User(1, "Alice"));
repository.GetById(Any()).Returns(new User(0, "Anyone"));
```

In both libraries, the most recently added matching setup wins.

### Computed Return Values

`ReturnsLazily` maps to `Returns` with a lambda. The parameters are the method's arguments.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
A.CallTo(() => repository.GetById(A<int>._)).ReturnsLazily((int id) => new User(id, "Generated"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(Any()).Returns((int id) => new User(id, "Generated"));
```

### Async Methods

FakeItEasy accepts either a value or a task for async members. In TUnit.Mocks, `Returns` wraps the value in `Task<T>` or `ValueTask<T>` for you.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
A.CallTo(() => repository.GetByIdAsync(1)).Returns(new User(1, "Alice"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetByIdAsync(1).Returns(new User(1, "Alice"));
```

To return a task you create yourself, such as one from a `TaskCompletionSource`, use `ReturnsAsync(task)`.

### Sequences

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
A.CallTo(() => repository.GetById(1))
    .Throws<InvalidOperationException>().Once()
    .Then.ReturnsNextFromSequence(new User(1, "First"), new User(1, "Second"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(1)
    .Throws<InvalidOperationException>()
    .Then()
    .ReturnsSequentially(new User(1, "First"), new User(1, "Second"));
```

When a FakeItEasy sequence ends, later calls fall back to the fake's default behavior. In TUnit.Mocks, the last behavior repeats. In this example, every call after the third call returns `"Second"`.

FakeItEasy limits a behavior with `.Once()`, `.Twice()`, or `.NumberOfTimes(n)`. TUnit.Mocks has no repeat count: each `.Then()` step applies to one call. Without `.Then()`, chained behaviors apply to the same call.

## Exceptions

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
A.CallTo(() => repository.GetById(-1)).Throws(new ArgumentOutOfRangeException("id"));
A.CallTo(() => repository.GetByIdAsync(-1)).ThrowsAsync(new InvalidOperationException());
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(-1).Throws(new ArgumentOutOfRangeException("id"));
repository.GetByIdAsync(-1).Throws<InvalidOperationException>();
```

TUnit.Mocks has no `ThrowsAsync`. For a method that returns `Task` or `ValueTask`, `Throws` returns a faulted task. It does not throw synchronously.

## Callbacks and Void Methods

`Invokes` maps to `Callback`. Declare the lambda parameter types to receive the method's arguments.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
var saved = new List<User>();
A.CallTo(() => repository.Save(A<User>._)).Invokes((User user) => saved.Add(user));
A.CallTo(() => repository.Save(A<User>.That.Matches(u => u.Id < 0))).Throws(new ArgumentException("Invalid id"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
var saved = new List<User>();
repository.Save(Any()).Callback((User user) => saved.Add(user));
repository.Save(user => user.Id < 0).Throws(new ArgumentException("Invalid id"));
```

`DoesNothing()` has no direct equivalent. In strict mode, a void setup with no chained behavior allows the call: `repository.Save(Any());`.

## Argument Constraints

TUnit.Mocks imports its matchers globally. A raw value is an exact match, and a lambda is a predicate.

| FakeItEasy | TUnit.Mocks |
|---|---|
| `A<int>.Ignored` / `A<int>._` | `Any()` or `Any<int>()` |
| `5` | `5` or `Is(5)` |
| `A<int>.That.Matches(x => x > 0)` | `x => x > 0` or `Is<int>(x => x > 0)` |
| `A<int>.That.IsEqualTo(5)` | `5` or `Is(5)` |
| `A<string>.That.IsNull()` | `IsNull<string>()` |
| `A<string>.That.IsNotNull()` | `IsNotNull<string>()` |
| `A<int>.That.Not.IsEqualTo(0)` | `Not(Is(0))` |
| `A<List<int>>.That.Contains(5)` | `Contains<List<int>, int>(5)` |
| `A<List<int>>.That.IsEmpty()` | `IsEmpty<List<int>>()` |
| `A<List<int>>.That.IsSameSequenceAs(expected)` | `SequenceEquals<List<int>, int>(expected)` |
| `A<string>.That.StartsWith("a")` | `s => s.StartsWith("a")` |
| `.WithAnyArguments()` | `Any()` for each parameter, or `AnyArgs()` where generated |
| `.WhenArgumentsMatch(args => ...)` | A lambda matcher on each parameter |

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
A.CallTo(() => repository.GetById(A<int>.That.Matches(id => id > 100))).Returns(new User(101, "Admin"));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.GetById(id => id > 100).Returns(new User(101, "Admin"));
```

To replace `.WithAnyArguments()`: pass `Any()` for each parameter. Some methods also get an `AnyArgs()` shortcut that replaces all of them. It is not generated for overloaded or generic methods, for methods with fewer than two matchable parameters, or for methods with `out`, `ref`, or ref-struct parameters. See [when the shortcut is generated](../../writing-tests/mocking/argument-matchers.md#anyargs--match-every-parameter-with-one-token).

### Capturing Arguments

FakeItEasy captures arguments with `Captured<T>` or in `Invokes`. In TUnit.Mocks, every matcher records the values it matches. Store the matcher in a variable and read it after the code under test runs.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
var savedUser = A.Captured<User>();
A.CallTo(() => repository.Save(savedUser._)).DoesNothing();
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

See [Argument Matchers](../../writing-tests/mocking/argument-matchers.md) for range, regex, and custom matchers.

## Out and Ref Parameters

FakeItEasy assigns `out` and `ref` values by position with `AssignsOutAndRefParameters`. TUnit.Mocks leaves `out` parameters out of the setup signature and generates a typed `SetsOut{ParameterName}` method for each one.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
string ignored;
A.CallTo(() => repository.TryGetName(1, out ignored))
    .Returns(true)
    .AssignsOutAndRefParameters("Alice");
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

FakeItEasy fakes behave like auto-properties: a value you set is returned by the getter. TUnit.Mocks properties return defaults until you configure them. Call `SetupAllProperties()` to get FakeItEasy's behavior.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
A.CallTo(() => repository.ConnectionName).Returns("primary");
A.CallToSet(() => repository.ConnectionName).To("replica").Throws<InvalidOperationException>();
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.ConnectionName.Returns("primary");
repository.ConnectionName.Set("replica").Throws<InvalidOperationException>();

// Opt in to auto-property behavior
var tracked = IUserRepository.Mock();
tracked.SetupAllProperties();
tracked.Object.ConnectionName = "replica"; // the getter now returns "replica"
```

Setups made with `Returns` take precedence over values stored by `SetupAllProperties()`.

## Verifying Calls

Remove `A.CallTo(() => ...)`, call the member on the mock, and replace `MustHaveHappened` with `WasCalled`.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();

A.CallTo(() => repository.Save(A<User>.That.Matches(u => u.Name == "Alice"))).MustHaveHappened();
A.CallTo(() => repository.GetById(1)).MustHaveHappenedTwiceExactly();
A.CallTo(() => repository.Save(A<User>._)).MustNotHaveHappened();
A.CallToSet(() => repository.ConnectionName).To("replica").MustHaveHappenedOnceExactly();
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();

repository.Save(user => user.Name == "Alice").WasCalled();
repository.GetById(1).WasCalled(Times.Exactly(2));
repository.Save(Any()).WasNeverCalled();
repository.ConnectionName.Set("replica").WasCalled(Times.Once);
```

| FakeItEasy | TUnit.Mocks |
|---|---|
| `MustHaveHappened()` | `WasCalled()` |
| `MustHaveHappenedOnceExactly()` | `WasCalled(Times.Once)` |
| `MustHaveHappenedTwiceExactly()` | `WasCalled(Times.Exactly(2))` |
| `MustHaveHappenedANumberOfTimesMatching(n => n == 3)` | `WasCalled(Times.Exactly(3))` |
| `MustHaveHappened(3, Times.OrMore)` | `WasCalled(Times.AtLeast(3))` |
| `MustHaveHappened(3, Times.OrLess)` | `WasCalled(Times.AtMost(3))` |
| `MustNotHaveHappened()` | `WasNeverCalled()` |

`FakeItEasy.Times` and `TUnit.Mocks.Times` are different types. In a migrated file, `Times` refers to the TUnit.Mocks type.

In a TUnit test, you can also await the check as an assertion. This needs the separate `TUnit.Mocks.Assertions` package (`dotnet add package TUnit.Mocks.Assertions`) and `using TUnit.Mocks.Assertions;`:

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
await Assert.That(repository.GetById(1)).WasCalled(Times.Once);
```

### Call Order

FakeItEasy chains ordered checks with `.Then(...)`. TUnit.Mocks groups them in `Mock.VerifyInOrder`, which works across several mocks.

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();

A.CallTo(() => repository.GetById(1)).MustHaveHappened()
    .Then(A.CallTo(() => repository.Save(A<User>._)).MustHaveHappened());
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

### Checks FakeItEasy Does Not Have

TUnit.Mocks also provides `mock.VerifyAll()`, which fails if a setup was never used, and `mock.VerifyNoOtherCalls()`, which fails if a call was not verified. See [Verification](../../writing-tests/mocking/verification.md).

## Events

```csharp
// FakeItEasy
var repository = A.Fake<IUserRepository>();
repository.UserSaved += Raise.With(new UserSavedEventArgs(new User(1, "Alice")));
```

```csharp
// TUnit.Mocks
var repository = IUserRepository.Mock();
repository.RaiseUserSaved(new UserSavedEventArgs(new User(1, "Alice")));
```

TUnit.Mocks generates a `Raise{EventName}` method for each event. To raise an event when a method is called, chain `.Raises{EventName}(...)` on a setup. See [Events](../../writing-tests/mocking/advanced.md#events).

## Classes

A FakeItEasy fake of a class does not call the base implementation unless you use `CallsBaseMethod()` or `options.CallsBaseMethods()`. A TUnit.Mocks class mock always calls the base implementation for unconfigured virtual members.

```csharp
// FakeItEasy
var calculator = A.Fake<PriceCalculator>(options => options.CallsBaseMethods());
A.CallTo(() => calculator.Discount(A<decimal>._)).Returns(5m);
```

```csharp
// TUnit.Mocks
var calculator = PriceCalculator.Mock();
calculator.Discount(Any()).Returns(5m);

decimal tax = calculator.Object.Tax(100m); // base implementation: 20
```

If a test depends on FakeItEasy's default behavior, configure each virtual member that the test calls. TUnit.Mocks can also configure `protected` virtual and abstract members with the same syntax as public members.

## Other Features

| FakeItEasy | TUnit.Mocks |
|---|---|
| Members that return interfaces return fakes | Loose mocks return auto-mocks; use `Mock.Get(instance)` to configure one |
| `A.Dummy<T>()` | No equivalent; use `T.Mock()` or a real value |
| `A.Fake<Func<int, string>>()` | `Mock.OfDelegate<Func<int, string>>()` |
| `A.CallTo(fake).Where(call => ...)` | No equivalent; configure each member |
| `Fake.GetCalls(fake)` | `mock.Invocations` (a list of `CallRecord`) |
| `Fake.ClearRecordedCalls(fake)` | `mock.Reset()`; it also clears setups and state |

See [Advanced Features](../../writing-tests/mocking/advanced.md) for state machines, diagnostics, custom default values, and `MockRepository`.

## Running Both Libraries Side by Side

TUnit.Mocks adds these global usings to the project:

- `TUnit.Mocks`
- `TUnit.Mocks.Arguments`
- `static TUnit.Mocks.Arguments.Arg`
- `TUnit.Mocks.Generated`

Most FakeItEasy names, such as `A`, `Fake`, and `Raise`, do not conflict with them. `Times` does: in a file that has `using FakeItEasy;`, `Times.OrMore` causes error `CS0104`. If you migrate one file at a time, use one of these fixes:

- In files that still use FakeItEasy, add `using Times = FakeItEasy.Times;`.
- Or set `<TUnitMockImplicitUsings>disable</TUnitMockImplicitUsings>` in the project and add the four usings above to each migrated file.

When no file uses FakeItEasy, remove the `FakeItEasy` package reference. Also remove `FakeItEasy.Analyzer.CSharp` if the project uses it.

## Native AOT

FakeItEasy creates proxies at runtime with Castle DynamicProxy and `Reflection.Emit`, which Native AOT does not support. TUnit.Mocks generates mocks at compile time, so the migrated tests can run in a Native AOT or trimmed test application. See [AOT compatibility](../../writing-tests/aot.md).

## Next Steps

- [Setup and stubbing](../../writing-tests/mocking/setup.md)
- [Verification](../../writing-tests/mocking/verification.md)
- [Advanced features](../../writing-tests/mocking/advanced.md)
