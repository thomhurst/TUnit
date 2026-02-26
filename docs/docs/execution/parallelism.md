---
sidebar_position: 10
---

# Controlling Parallelism

TUnit runs all tests in parallel by default. This page covers three attributes that give you fine-grained control when you need it.

:::performance
Parallel execution is a major contributor to TUnit's speed advantage. See the [performance benchmarks](/docs/benchmarks) for real-world data.
:::

## Default Behavior

With no attributes, every test is eligible to run concurrently. The .NET thread pool determines how many execute at once. For most test suites this is the fastest option and requires no configuration.

## `[NotInParallel]` — Disabling Parallelism

Add `[NotInParallel]` to prevent a test from running at the same time as other constrained tests.

It accepts an optional array of **constraint keys**. Tests that share any key will never overlap. Tests with no common key may still run concurrently.

If no keys are supplied, the test runs completely alone — no other test executes at the same time.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private const string DatabaseTest = "DatabaseTest";
    private const string RegistrationTest = "RegistrationTest";
    private const string ParallelTest = "ParallelTest";

    [Test]
    [NotInParallel(DatabaseTest)]
    public async Task MyTest()
    {
        var count = await Database.GetUserCountAsync();
        await Assert.That(count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    [NotInParallel(new[] { DatabaseTest, RegistrationTest })]
    public async Task MyTest2()
    {
        var user = await Database.CreateUserAsync("alice");
        await Assert.That(user.Name).IsEqualTo("alice");
    }

    [Test]
    [NotInParallel(ParallelTest)]
    public async Task MyTest3()
    {
        var result = await Api.PingAsync();
        await Assert.That(result.IsSuccess).IsTrue();
    }
}
```

`MyTest` and `MyTest2` share the `DatabaseTest` key, so they never overlap. `MyTest3` has a different key and may run alongside either of them.

### Global `[NotInParallel]`

Disable parallelism for every test in an assembly (run all tests sequentially):

```csharp
[assembly: NotInParallel]
```

## `[ParallelGroup]` — Grouping Tests into Phases

`[ParallelGroup("key")]` batches classes into groups. Tests within the same group run in parallel with each other, but no tests from other groups run at the same time. The engine finishes one group entirely before starting the next.

```csharp
using TUnit.Core;

namespace MyTestProject;

[ParallelGroup("Database")]
public class UserRepositoryTests
{
    [Test]
    public async Task Create_User()
    {
        var user = await UserRepository.CreateAsync("alice");
        await Assert.That(user.Name).IsEqualTo("alice");
    }

    [Test]
    public async Task Delete_User()
    {
        await UserRepository.DeleteAsync("bob");
        var exists = await UserRepository.ExistsAsync("bob");
        await Assert.That(exists).IsFalse();
    }
}

[ParallelGroup("Database")]
public class OrderRepositoryTests
{
    [Test]
    public async Task Create_Order()
    {
        var order = await OrderRepository.CreateAsync("item-1");
        await Assert.That(order.Id).IsNotNull();
    }
}

[ParallelGroup("ExternalApi")]
public class PaymentApiTests
{
    [Test]
    public async Task Charge_Succeeds()
    {
        var result = await PaymentApi.ChargeAsync(100);
        await Assert.That(result.Success).IsTrue();
    }
}
```

`UserRepositoryTests` and `OrderRepositoryTests` share the `"Database"` group, so all their tests run together. `PaymentApiTests` is in `"ExternalApi"` and runs in a separate phase.

Tests not assigned to any group run separately under normal parallel execution rules.

## `[ParallelLimiter<T>]` — Limiting Concurrent Test Count

`[ParallelLimiter<T>]` caps how many tests sharing the same limiter type can run concurrently. The generic type argument must implement `IParallelLimit` with a public parameterless constructor.

The limit is shared across **all** tests referencing the same `IParallelLimit` type. Tests with a different limiter type or no limiter are unaffected.

```csharp
using TUnit.Core;

namespace MyTestProject;

[ParallelLimiter<MyParallelLimit>]
public class MyTestClass
{
    [Test, Repeat(10)]
    public async Task MyTest()
    {
        await Assert.That(true).IsTrue();
    }

    [Test, Repeat(10)]
    public async Task MyTest2()
    {
        await Assert.That(1 + 1).IsEqualTo(2);
    }
}

public record MyParallelLimit : IParallelLimit
{
    public int Limit => 2;
}
```

With a limit of `2`, at most two of these 20 test invocations execute at the same time.

### Assembly-Level Limiter

```csharp
[assembly: ParallelLimiter<MyParallelLimit>]
```

More specific attributes override less specific ones. Precedence: Method > Class > Assembly.

## When to Use Which

| Scenario | Attribute |
|---|---|
| Tests must never overlap (e.g., shared database state) | `[NotInParallel("key")]` |
| Groups of tests can overlap internally but must be isolated from other groups | `[ParallelGroup("key")]` |
| Tests can overlap but you need to cap concurrency (e.g., limited external connections) | `[ParallelLimiter<T>]` |
| Disable all parallelism for an assembly | `[assembly: NotInParallel]` |

## Caveats

- If a test uses `[DependsOn(nameof(OtherTest))]` and the dependency has a different parallel limiter or group, ordering is not guaranteed.
- `[NotInParallel]` without keys is the most restrictive — the test runs completely alone. Use constraint keys when possible to allow unrelated tests to proceed.
