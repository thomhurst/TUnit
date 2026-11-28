---
sidebar_position: 10
---


# Controlling Parallelism

TUnit runs all tests in parallel by default. This page covers three attributes that give you fine-grained control when you need it.

:::performance
Parallel execution is a major contributor to TUnit's speed advantage. See the [performance benchmarks](/docs/benchmarks) for real-world data.
:::

## Default Behavior

With no attributes, every test is eligible to run concurrently. At most 2 x the processor count run at once. Change this limit with `--maximum-parallel-tests`, the `TUNIT_MAX_PARALLEL_TESTS` environment variable or `context.Settings.Parallelism.MaximumParallelTests`. For most test suites the default is the fastest option and requires no configuration. If your tests share a resource with limited capacity, such as a database connection pool, lower the limit for those tests with `[ParallelLimiter<T>]`: extra concurrent tests add no throughput and only wait inside their test bodies, which counts toward `[Timeout]`.

:::note Async tests and the thread pool
TUnit uses the standard .NET thread pool and, like most .NET applications, is susceptible to thread pool starvation. Blocking calls or heavy CPU-bound `Task.Run` work can delay async continuations in other tests and cause timed waits to expire.

By default, the pool adds threads beyond its minimum only about once or twice per second, so a burst of blocking tests can delay timers and `[Timeout]` handling in unrelated tests. To reduce this, TUnit watches the thread pool while tests run. If queued work waits for a thread while most CPUs are idle, TUnit raises the pool's minimum worker thread count to the maximum number of parallel tests plus one per processor. If the pool is still starved once it has grown that far (for example because keyed `[NotInParallel]` tests run alongside the parallel limit), TUnit doubles the minimum, up to 64 threads per processor. The raised minimum lasts for the rest of the run. Suites that do not block never trigger this, so they keep the default thread count. TUnit never lowers a minimum that you configured yourself. To turn this off, set `context.Settings.Execution.ThreadPoolTuningEnabled = false` in a `[Before(HookType.TestDiscovery)]` hook (see [Programmatic Configuration](../reference/programmatic-configuration.md)), or set the `TUNIT_DISABLE_THREADPOOL_TUNING=1` environment variable.

When a test times out, the error message includes a thread pool snapshot. If the pool looked saturated, the message says the timeout may come from blocking calls in other tests.

Use proper async throughout your tests: `await` asynchronous APIs instead of blocking with `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()`. Await asynchronous I/O directly rather than wrapping it in `Task.Run`; `Task.Run` still uses the same thread pool.

For heavy CPU-bound tests, use a shared [`ParallelLimiter<T>`](#parallellimitert--limiting-concurrent-test-count) to reduce concurrency for those tests. The limiter caps concurrent tests that share it, not the number of tasks each test creates.
:::

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
        await Assert.That(order.Id).IsNotEmptyGuid();
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

An explicit `[ParallelLimiter<T>]` always takes precedence over a limiter set programmatically through `TestRegisteredContext.SetParallelLimiter`, including one supplied by an executor. This precedence does not depend on registration callback order. Without an explicit attribute, the last programmatic limiter applies.

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
[assembly: ParallelLimiter<MyAssemblyParallelLimit>]

public record MyAssemblyParallelLimit : IParallelLimit
{
    public int Limit => 2;
}
```

More specific attributes override less specific ones. Precedence: Method > Class > Assembly.

## Setting Maximum Parallel Tests

You can cap the total number of concurrent tests globally using the command line or an environment variable:

```bash
# Command-line flag
dotnet run --project MyTests -- --maximum-parallel-tests 4

# Environment variable
TUNIT_MAX_PARALLEL_TESTS=4 dotnet run --project MyTests
```

You can also set this programmatically via `context.Settings.Parallelism.MaximumParallelTests` in a `[Before(HookType.TestDiscovery)]` hook. See the [Programmatic Configuration](/docs/reference/programmatic-configuration) reference for details.

## Adaptive Parallelism

A fixed limit suits some suites better than others. Tests that mostly await I/O leave the machine idle at 2 x the processor count, while tests that use a lot of memory can exhaust it at the same number. With adaptive parallelism, TUnit changes the limit while the run is in progress. It is off by default.

```bash
# Command-line flag
dotnet run --project MyTests -- --adaptive-parallelism

# Environment variable
TUNIT_ADAPTIVE_PARALLELISM=1 dotnet run --project MyTests
```

You can also set `context.Settings.Parallelism.Adaptive = true` in a `[Before(HookType.TestDiscovery)]` hook.

The run starts at the usual limit. From there TUnit:

- **Raises the limit** by half when tests are waiting for a slot, CPU use is below 80%, memory use is under 85% of the level the .NET runtime treats as high, and thread pool work is not queuing. It then measures completed tests per second. If the raise did not bring a fair share more (15% more for a 50% raise), TUnit undoes it and waits before trying again: 15 seconds at first, doubling up to 2 minutes.
- **Lowers the limit** by half when memory use reaches the runtime's high-memory level, and by a quarter when the CPUs are saturated while thread pool work queues for a thread. Running tests are never interrupted; the lower limit takes effect as they finish.
- **Returns to the starting limit** in steps once the pressure has eased.

The limit never goes above `--maximum-parallel-tests` when you set it, or 8 x the processor count when you do not. It never goes below a quarter of the starting limit.

Idle CPU alone does not raise the limit. Tests that wait on a shared resource, such as a database connection pool, leave the CPU idle, but running more of them only makes each one wait longer. Because TUnit keeps a raise only when it completes more tests per second, such suites stay at the starting limit. `[ParallelLimiter<T>]` is still the right tool when you know a group of tests shares limited capacity.

The summary at the end of the run reports the range the limit moved in, for example `Adaptive parallelism: the parallel test limit ranged from 16 to 64 over 4 changes and ended at 64`. Run with `--log-level Debug` to see each change and its reason. If a test fails only under adaptive parallelism, rerun with `--maximum-parallel-tests` set to the highest limit reported to reproduce that level of concurrency with a fixed limit.

TUnit reads memory use from the .NET runtime, which measures it at each garbage collection. Memory taken by child processes or containers is therefore noticed only at the test process's next collection. If your tests start memory-heavy processes, also set `--maximum-parallel-tests` to bound how far the limit can grow.

Adaptive parallelism requires .NET 8 or later and runs on Windows, Linux and macOS. Elsewhere the limit stays fixed.

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
