---
sidebar_position: 11
---

# Mocking

TUnit includes **TUnit.Mocks** (currently in **beta**), a source-generated, AOT-compatible mocking framework. Because mocks are generated at compile time, TUnit.Mocks works with Native AOT, trimming, and single-file publishing — unlike traditional mocking libraries that rely on runtime proxy generation.

## Key Features

- **Interfaces, abstract classes, and concrete classes** — mock any non-sealed reference type
- **Delegate mocking** — `Mock.OfDelegate<Func<int, string>>()` for testing delegate-accepting code
- **Wrap real objects** — `Mock.Wrap(realInstance)` to selectively override methods while delegating unconfigured calls to the real implementation
- **Recursive/auto mocking** — methods returning interfaces automatically return functional mocks instead of null (in loose mode)
- **Auto-raise events** — `.Raises("EventName", args)` on setup chains to trigger events when a method is called
- **Event subscription callbacks** — `mock.OnSubscribe("EventName", callback)` to react when handlers are added/removed
- **Multiple interfaces** — `Mock.Of<IFoo, IBar>()` to create a single mock implementing multiple interfaces
- **Sequential behaviors** — `.Returns(1).Then().Returns(2)` for different values on successive calls
- **Argument capture** — every `Arg<T>` matcher captures values implicitly (`arg.Values`, `arg.Latest`)
- **Ordered verification** — `Mock.VerifyInOrder(...)` with global sequence tracking across mocks
- **Strict and loose modes** — strict mode throws on unconfigured calls; loose mode returns smart defaults

## Using with Other Frameworks

You can also use any other .NET mocking library with TUnit, such as NSubstitute, Moq, FakeItEasy, or any other mocking framework that works with .NET.

See the [Mocking Framework Comparison](/docs/comparison/mocking-frameworks) page for a detailed feature comparison.