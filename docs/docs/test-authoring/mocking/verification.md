---
sidebar_position: 3
---

# Verification

Verification uses the same methods as setup — the chain method (`.WasCalled()`, `.WasNeverCalled()`) makes it a verification instead of a setup.

## Basic Verification

```csharp
// Verify a method was called at least once
mock.GetUser(42).WasCalled();

// Verify exact call count
mock.GetUser(42).WasCalled(Times.Once);

// Verify never called
mock.Delete(Arg.Any<int>()).WasNeverCalled();
```

### Times

| Expression | Matches |
|---|---|
| `Times.Once` | Exactly 1 call |
| `Times.Never` | Exactly 0 calls |
| `Times.AtLeastOnce` | 1 or more calls |
| `Times.Exactly(n)` | Exactly n calls |
| `Times.AtLeast(n)` | n or more calls |
| `Times.AtMost(n)` | n or fewer calls |
| `Times.Between(min, max)` | Between min and max calls (inclusive) |

### Custom Failure Messages

```csharp
mock.GetUser(42).WasCalled(Times.Once, "GetUser should be called once during initialization");
mock.Delete(Arg.Any<int>()).WasNeverCalled("Delete should not be called in read-only mode");
```

## Property Verification

Property verification mirrors the setup API — defaults to the **getter**:

```csharp
// Getter verification
mock.Name.WasCalled(Times.Once);           // getter called once
mock.Name.Getter.WasCalled(Times.Once);    // explicit — same as above
mock.Name.WasNeverCalled();                 // getter never accessed

// Setter verification — any value
mock.Count.Setter.WasCalled(Times.Exactly(3));
mock.Count.Setter.WasNeverCalled();

// Setter verification — specific value
mock.Count.Set(42).WasCalled(Times.Once);
mock.Count.Set(Arg.Is<int>(v => v > 0)).WasCalled(Times.AtLeast(1));
```

## Argument Matching in Verification

Verification uses the same `Arg<T>` matchers as setup:

```csharp
// Exact value
mock.GetUser(42).WasCalled(Times.Once);

// Any value
mock.GetUser(Arg.Any<int>()).WasCalled(Times.Exactly(3));

// Predicate
mock.GetUser(Arg.Is<int>(id => id > 0)).WasCalled(Times.AtLeast(1));
```

See [Argument Matchers](argument-matchers) for the full list of matchers.

## Ordered Verification

Verify calls occurred in a specific order **across one or more mocks**:

```csharp
Mock.VerifyInOrder(() =>
{
    mockLogger.Log("Starting").WasCalled();
    mockRepo.SaveAsync(Arg.Any<int>()).WasCalled();
    mockLogger.Log("Done").WasCalled();
});
```

If calls occurred out of order, `VerifyInOrder` throws with a message showing the actual sequence.

:::tip
`VerifyInOrder` uses a global sequence counter — it works across multiple independent mock instances, not just within a single mock.
:::

## VerifyAll

Verify that **every setup** was invoked at least once:

```csharp
mock.GetUser(Arg.Any<int>()).Returns(new User("Alice"));
mock.Delete(Arg.Any<int>());

svc.GetUser(1);
svc.Delete(2);

mock.VerifyAll(); // passes — both setups were invoked
```

If any setup was never called, `VerifyAll` throws listing the uninvoked setups.

## VerifyNoOtherCalls

Verify that all recorded calls have been explicitly verified:

```csharp
svc.GetUser(1);
svc.Delete(2);

mock.GetUser(1).WasCalled(Times.Once);
mock.Delete(2).WasCalled(Times.Once);

mock.VerifyNoOtherCalls(); // passes — all calls accounted for
```

If there are unverified calls, `VerifyNoOtherCalls` throws listing them.

## TUnit Assertion Integration

Use TUnit's `Assert.That` pipeline for assertion-style verification with better error messages:

```csharp
using TUnit.Mocks.Assertions;

await Assert.That(mock.GetUser(42)).WasCalled(Times.Once);
await Assert.That(mock.Delete(Arg.Any<int>())).WasNeverCalled();

// Property verification through assertions
await Assert.That(mock.Name).WasCalled(Times.Once);
```

This integrates with TUnit's assertion engine — failures appear as assertion errors with expression trees in the output.

## Inspecting Calls

Access the raw call history for custom inspection:

```csharp
var calls = mock.Invocations;

await Assert.That(calls).HasCount().EqualTo(3);
await Assert.That(calls[0].MemberName).IsEqualTo("GetUser");
```

Each `CallRecord` contains the member name, arguments, and sequence number.
