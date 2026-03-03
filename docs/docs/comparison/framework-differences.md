# Framework Differences

TUnit is inspired by NUnit and xUnit — they're excellent frameworks that have served the .NET community well. TUnit was built to address some pain points around parallelism, lifecycle hooks, test isolation, and extensibility.

## Quick Comparison

| Framework | Class Attribute | Method Attribute |
|-----------|----------------|------------------|
| **TUnit** | None needed | `[Test]` |
| **MSTest** | `[TestClass]` | `[TestMethod]` |
| **NUnit** | `[TestFixture]` (optional) | `[Test]` |
| **xUnit** | None needed | `[Fact]` / `[Theory]` |

## xUnit

### Parallel test limiting

xUnit lets you limit the thread count, but that doesn't map 1-to-1 to async tests. A single thread can run multiple async tests when they yield, so limiting threads doesn't actually limit how many tests run concurrently. If you're spawning browser instances for UI tests, for example, this matters — too many at once and your system grinds to a halt.

TUnit lets you limit the actual number of concurrent tests: `--maximum-parallel-tests 8`

### Setup and teardown

xUnit uses constructors and `IDisposable` for setup/teardown (TUnit supports this too if you prefer it). For async scenarios, there's `IAsyncLifetime`. This works fine until inheritance gets involved — your derived class has to hide the base members, reimplement the interface, and manually call base methods.

Teardown has another problem: you can't implement `IDisposable` multiple times. If you need to guarantee multiple cleanup steps run even when one fails, you end up writing nested try/catches.

In TUnit, you can declare multiple `[After(Test)]` methods. They're all guaranteed to run even if a previous one threw, and any exceptions are aggregated and thrown afterwards. Setup and teardown methods are also collected through the inheritance chain and run in a sensible order — base setups first, then derived; derived cleanups first, then base.

### Assembly-level hooks

xUnit doesn't have a straightforward way to run code before or after an assembly's tests. Spinning up a shared in-memory server, for instance, requires workarounds.

In TUnit, it's a static method with `[Before(Assembly)]` and `[After(Assembly)]`.

### TestContext

Sometimes you need to know whether a test failed — taking a screenshot in a UI test teardown, for example. xUnit doesn't have a native way to check test state from a teardown method.

In TUnit, you can inject a `TestContext` into your teardown method, or call `TestContext.Current`.

### Assertions

xUnit assertions have the classic problem of unclear argument order:

```csharp
var one = 2;
Assert.Equal(1, one);   // is 1 the expected or actual?
Assert.Equal(one, 1);   // ...or is it this way round?
```

TUnit uses a fluent syntax that reads naturally: `await Assert.That(one).IsEqualTo(1);`

## NUnit

### Shared test class instances

This catches a lot of people out. NUnit's default behaviour is to run all tests in a class against a single instance. If you store state in fields or properties, it leaks between tests. Tests that pass individually start failing when run together, and the cause isn't obvious.

TUnit creates a new instance for every test, with no way to opt out. If you need shared state, use `static` fields — that makes the sharing explicit and visible to anyone reading the code.

### Dynamic data and filtering

Consider a multi-tenanted test suite where tests repeat with different tenants injected via `[TestFixtureSource]`. A natural next step is filtering by tenant. In NUnit, you might try using `IApplyToTest` to set a property based on the constructor argument, but it doesn't work — tests are enumerated at startup before the fixture source provides its values.

Because TUnit discovers tests via source generation, constructor arguments are available upfront. You can set properties with `ITestDiscoveryEvent` and filter with `--treenode-filter /*/*/*/*[Tenant=MyTenant]`.

### Attribute scope

NUnit's `[Repeat]` and `[Retry]` only work on test methods. Want to retry every test in a class? Or set a default for the whole assembly? You can't.

In TUnit, most attributes work at test, class, and assembly level. Test takes highest priority, then class, then assembly — so you can set defaults broadly and override them where needed.

### Assertions

NUnit's constraint model heavily influenced TUnit's assertions. The difference is compile-time safety. Nothing stops you writing `Assert.That("hello", Is.Negative)` or `Assert.That(true, Throws.ArgumentException)` in NUnit — these compile fine but make no sense. There are analyzers that help, but they're optional.

TUnit assertions are built on the type system. Assertions are extension methods on the relevant types, so intellisense only shows you what actually applies. You can't accidentally check if a string is negative because that method doesn't exist on strings.

## General

### Source generation, Native AOT, and single-file support

TUnit discovers tests at compile time through source generation rather than runtime reflection. You can inspect the generated code yourself. This also means you can build test projects as Native AOT or single-file applications — something NUnit and xUnit don't currently support.

### Lifecycle hooks

Other frameworks give you test-level and maybe class-level hooks. TUnit provides hooks at every level: test, class, assembly, test session, and test discovery. You can also use `[BeforeEvery(...)]` and `[AfterEvery(...)]` variants for cross-cutting concerns that apply globally.

See the [hooks documentation](/docs/writing-tests/hooks) for the full list.

### Test dependencies

In other frameworks, running tests in a specific order usually means disabling parallelism and numbering tests with `[Order]`.

TUnit has `[DependsOn(...)]` — a test waits for its dependencies to finish, without disabling parallelism for everything else:

```csharp
[Test]
public async Task Test1() { ... }

[Test]
public async Task Test2() { ... }

[Test]
[DependsOn(nameof(Test1))]
[DependsOn(nameof(Test2))]
public async Task Test3() { ... }
```

### Data injection

Many data injection mechanisms in xUnit and NUnit work for either the method or the class, but not both. In TUnit, `[Arguments(...)]`, `[Matrix(...)]`, `[MethodDataSource(...)]`, and other data attributes work on both classes and test methods.
