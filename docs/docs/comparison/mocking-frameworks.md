---
sidebar_position: 3
---

# Mocking Framework Comparison

TUnit.Mocks is a **source-generated, AOT-compatible** mocking framework built into TUnit. It is currently in **beta** — the API may change before the stable release. This page compares it against the three most popular .NET mocking libraries: [Moq](https://github.com/moq/moq4), [NSubstitute](https://nsubstitute.github.io/), and [FakeItEasy](https://fakeiteasy.github.io/).

## Why TUnit.Mocks?

Every popular .NET mocking framework relies on **Castle DynamicProxy** to generate proxy classes at runtime via IL emission. This fundamentally conflicts with:

- **Native AOT** — no runtime code generation
- **Trimming** — the linker can't analyze dynamically-generated types
- **Single-file deployment** — proxy assemblies must be emitted to disk or memory

TUnit.Mocks solves this by generating mock implementations at **compile time** via Roslyn source generators. The generated code is ordinary C# — fully visible, debuggable, and compatible with AOT, trimming, and single-file publishing.

## Architecture Comparison

| | **TUnit.Mocks** | **Moq** | **NSubstitute** | **FakeItEasy** |
|---|---|---|---|---|
| Proxy mechanism | **Source generated** | Castle DynamicProxy | Castle DynamicProxy | Castle DynamicProxy |
| AOT compatible | **Yes** | No | No | No |
| Trimming safe | **Yes** | No | No | No |
| Single-file safe | **Yes** | No | No | No |
| Built-in analyzers | **Yes** | No | Community | No |
| Setup style | Fluent extension methods | Expression trees | Direct call syntax | `A.CallTo()` expressions |

## Quick Syntax Comparison

### Creating a Mock

```csharp
// TUnit.Mocks
var mock = Mock.Of<ICalculator>();
ICalculator calc = mock; // implicit conversion — no .Object needed

// Moq
var mock = new Mock<ICalculator>();
ICalculator calc = mock.Object; // .Object required

// NSubstitute
var calc = Substitute.For<ICalculator>();

// FakeItEasy
var calc = A.Fake<ICalculator>();
```

### Setting Up Return Values

```csharp
// TUnit.Mocks — type-safe extension methods, async auto-unwrapped
mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);
mock.Setup.GetValueAsync(Arg.Any<string>()).Returns("hello"); // no ReturnsAsync needed

// Moq — expression trees
mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(42);
mock.Setup(x => x.GetValueAsync(It.IsAny<string>())).ReturnsAsync("hello");

// NSubstitute — direct call syntax
calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);
calc.GetValueAsync(Arg.Any<string>()).Returns("hello");

// FakeItEasy — A.CallTo expressions
A.CallTo(() => calc.Add(A<int>._, A<int>._)).Returns(42);
A.CallTo(() => calc.GetValueAsync(A<string>._)).Returns("hello");
```

### Verification

```csharp
// TUnit.Mocks
mock.Verify!.Add(1, 2).WasCalled(Times.Once);
mock.Verify!.Reset().WasNeverCalled();

// Moq
mock.Verify(x => x.Add(1, 2), Times.Once());
mock.Verify(x => x.Reset(), Times.Never());

// NSubstitute
calc.Received(1).Add(1, 2);
calc.DidNotReceive().Reset();

// FakeItEasy
A.CallTo(() => calc.Add(1, 2)).MustHaveHappenedOnceExactly();
A.CallTo(() => calc.Reset()).MustNotHaveHappened();
```

### Ordered Verification

```csharp
// TUnit.Mocks — cross-mock ordered verification with global sequence tracking
Mock.VerifyInOrder(() =>
{
    mockLogger.Verify!.Log(Arg.Any<string>()).WasCalled();
    mockRepo.Verify!.SaveAsync(Arg.Any<int>()).WasCalled();
    mockLogger.Verify!.Log("Done").WasCalled();
});

// Moq — MockSequence
var sequence = new MockSequence();
mockLogger.InSequence(sequence).Setup(x => x.Log(It.IsAny<string>()));
mockRepo.InSequence(sequence).Setup(x => x.SaveAsync(It.IsAny<int>()));

// NSubstitute
Received.InOrder(() =>
{
    logger.Log(Arg.Any<string>());
    repo.SaveAsync(Arg.Any<int>());
    logger.Log("Done");
});

// FakeItEasy — ordered assertion scope (less common)
```

### Sequential Behaviors

```csharp
// TUnit.Mocks — .Then() chaining
mock.Setup.GetValue(Arg.Any<string>())
    .Throws<InvalidOperationException>()
    .Then()
    .Returns("recovered");

// Moq — SetupSequence
mock.SetupSequence(x => x.GetValue(It.IsAny<string>()))
    .Throws<InvalidOperationException>()
    .Returns("recovered");

// NSubstitute
calc.GetValue(Arg.Any<string>())
    .Returns(x => throw new InvalidOperationException(), x => "recovered");

// FakeItEasy
A.CallTo(() => calc.GetValue(A<string>._))
    .Throws<InvalidOperationException>().Once()
    .Then.Returns("recovered");
```

### Argument Capture

```csharp
// TUnit.Mocks — every Arg captures implicitly
var name = Arg.Any<string>();
mock.Setup.Log(name);
// ... exercise code ...
var allValues = name.Values; // IReadOnlyList<string?>
var latest = name.Latest;

// Moq — Capture.In
var captured = new List<string>();
mock.Setup(x => x.Log(Capture.In(captured)));

// NSubstitute — Arg.Do
var captured = new List<string>();
calc.Log(Arg.Do<string>(x => captured.Add(x)));

// FakeItEasy — Captured<T>
var captured = A.Captured<string>();
A.CallTo(() => calc.Log(captured._)).DoesNothing();
```

## Feature Matrix

### Setup / Stubbing

| Feature | TUnit.Mocks | Moq | NSubstitute | FakeItEasy |
|---|:---:|:---:|:---:|:---:|
| Fixed returns | Yes | Yes | Yes | Yes |
| Computed/factory returns | Yes | Yes | Yes | Yes |
| Sequential returns | Yes | Yes | Yes | Yes |
| Async auto-unwrap | **Yes** | No (`ReturnsAsync`) | Yes | Yes |
| Callbacks | Yes | Yes | Yes | Yes |
| Chained `.Then()` behaviors | Yes | `SetupSequence` | `Callback.First/Then` | `.Once().Then` |
| Throws (sync & async) | Yes | Yes | Yes | Yes |
| Out parameters | Yes | Yes | Yes | Yes |
| Ref parameters | Yes | Yes | Yes | Yes |
| Property getter/setter | Yes | Yes | Yes | Yes |
| Auto-track all properties | **Yes** (auto in loose) | `SetupAllProperties` | Auto | No |
| Recursive/auto mocking | **Yes** | Yes | Yes | Yes |
| Wrap real object | **Yes** (`Mock.Wrap`) | `CallBase` | `ForPartsOf` | `Wrapping()` |
| LINQ to Mocks | No | `Mock.Of<T>(x => ...)` | No | No |

### Verification

| Feature | TUnit.Mocks | Moq | NSubstitute | FakeItEasy |
|---|:---:|:---:|:---:|:---:|
| Call count (Once, Never, Exactly, AtLeast, AtMost, Between) | Yes | Yes | Yes | Yes |
| Ordered verification | Yes | Yes | Yes | Yes |
| VerifyNoOtherCalls | **Yes** | Yes | No | No |
| VerifyAll (all setups invoked) | **Yes** | Yes | No | No |
| Property verification | Yes | Yes | Yes | Yes |
| Event subscription verify | **Yes** | Yes | Yes | No |
| Raw call inspection | **Yes** (`.Invocations`) | `.Invocations` | `ReceivedCalls()` | `Fake.GetCalls()` |

### Argument Matching

| Feature | TUnit.Mocks | Moq | NSubstitute | FakeItEasy |
|---|:---:|:---:|:---:|:---:|
| Any | Yes | Yes | Yes | Yes |
| Exact value | Yes | Yes | Yes | Yes |
| Predicate | Yes | Yes | Yes | Yes |
| Null / NotNull | Yes | Predicate | Predicate | Yes |
| Capture | Yes | Yes | `Arg.Do` | Yes |
| Regex | **Yes** | Yes | No | No |
| Collection matchers | **Yes** | No | No | Yes |
| Custom matcher types | **Yes** | Yes | No | No |
| Implicit value conversion | **Yes** | N/A | N/A | N/A |

### Mock Targets

| Feature | TUnit.Mocks | Moq | NSubstitute | FakeItEasy |
|---|:---:|:---:|:---:|:---:|
| Interfaces | Yes | Yes | Yes | Yes |
| Abstract classes | Yes | Yes | Yes | Yes |
| Concrete classes (virtual) | Yes | Yes | Yes | Yes |
| Sealed classes | No | No | No | No |
| Static methods | No | No | No | No |
| Generic types & methods | Yes | Yes | Yes | Yes |
| Protected members | **Yes** (source-gen access) | **Explicit API** | Virtual only | Virtual only |
| Internal types | Yes | Yes | Yes | Yes |
| Delegates | **Yes** | No | Yes | Yes |
| Multiple interfaces | **Yes** | Yes | Yes | Yes |
| Constructor args (partial) | Yes | Yes | Yes | Yes |

### Mock Behavior

| Feature | TUnit.Mocks | Moq | NSubstitute | FakeItEasy |
|---|:---:|:---:|:---:|:---:|
| Loose mode | Yes (default) | Yes (default) | Yes (only) | Yes (default) |
| Strict mode | Yes | Yes | No | Yes |
| Smart defaults | Yes (per-type) | `DefaultValue.Empty/Mock` | Auto-substitutes | Dummies |
| Reset | Yes | Yes | Yes | Yes |

### Events

| Feature | TUnit.Mocks | Moq | NSubstitute | FakeItEasy |
|---|:---:|:---:|:---:|:---:|
| Raise events | Yes | Yes | Yes | Yes |
| Auto-raise on method call | **Yes** (`.Raises()`) | Yes | No | No |
| Non-EventHandler delegates | **Yes** | Yes | Yes | Yes |
| Event subscription setup | **Yes** | Yes | No | Yes |

### Quality of Life

| Feature | TUnit.Mocks | Moq | NSubstitute | FakeItEasy |
|---|:---:|:---:|:---:|:---:|
| Implicit `T` conversion | **Yes** | No (`.Object`) | Yes | Yes |
| Thread safety | **Explicit** (locks + ConcurrentQueue) | Not documented | Thread-local | Known issues |
| Built-in analyzers | **Yes** | No | Community | No |
| Async setup (no special API) | **Yes** | No | Yes | Yes |
| Mock repository / batch verify | **Yes** | Yes | No | No |

## Summary

**TUnit.Mocks excels at:**
- **Full feature parity** — matches or exceeds Moq, NSubstitute, and FakeItEasy in every category
- AOT compatibility and trimming — the **only** source-generated option
- Zero runtime overhead — no reflection, no dynamic proxies
- Async ergonomics — `.Returns(value)` just works for `Task<T>` / `ValueTask<T>`
- Implicit conversion — `T obj = mock` with no `.Object` call
- Recursive/auto mocking — interface return types auto-mocked with full chain support
- Delegate mocking — `Mock.OfDelegate<Func<int, string>>()`
- Wrap real objects — `Mock.Wrap(realInstance)` with selective overrides
- Built-in analyzers — catch misuse at compile time
- Thread safety — designed for parallel test execution from the start

**Choose Moq if you need:**
- Expression-tree setup syntax (`x => x.Method(...)`)
- LINQ to Mocks (`Mock.Of<T>(x => x.Prop == value)`)
- The largest community and ecosystem

**Choose NSubstitute if you need:**
- Minimal ceremony — the substitute IS the type (no `.Object`)
- The simplest syntax with no lambdas needed

**Choose FakeItEasy if you need:**
- `Captured<T>` with deep-copy support
