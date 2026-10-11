# Controlling Parallelism

TUnit runs all tests in parallel by default. This page covers three attributes that give you fine-grained control when you need it.

Performance

Parallel execution is a major contributor to TUnit's speed advantage. See the [performance benchmarks](/docs/benchmarks/.md) for real-world data.

## Default Behavior[​](#default-behavior "Direct link to Default Behavior")

With no attributes, every test is eligible to run concurrently. The .NET thread pool determines how many execute at once. For most test suites this is the fastest option and requires no configuration.

Async tests and the thread pool

TUnit uses the standard .NET thread pool and, like most .NET applications, is susceptible to thread pool starvation. Blocking calls or heavy CPU-bound `Task.Run` work can delay async continuations in other tests and cause timed waits to expire.

By default, the pool adds threads beyond its minimum only about once or twice per second, so a burst of blocking tests can delay timers and `[Timeout]` handling in unrelated tests. To reduce this, TUnit watches the thread pool while tests run. If queued work waits for a thread while most CPUs are idle, TUnit raises the pool's minimum worker thread count to the maximum number of parallel tests plus one per processor. If the pool is still starved once it has grown that far (for example because keyed `[NotInParallel]` tests run alongside the parallel limit), TUnit doubles the minimum, up to 64 threads per processor. The raised minimum lasts for the rest of the run. Suites that do not block never trigger this, so they keep the default thread count. TUnit never lowers a minimum that you configured yourself. To turn this off, set `context.Settings.Execution.ThreadPoolTuningEnabled = false` in a `[Before(HookType.TestDiscovery)]` hook (see [Programmatic Configuration](/docs/reference/programmatic-configuration.md)), or set the `TUNIT_DISABLE_THREADPOOL_TUNING=1` environment variable.

When a test times out, the error message includes a thread pool snapshot. If the pool looked saturated, the message says the timeout may come from blocking calls in other tests.

Use proper async throughout your tests: `await` asynchronous APIs instead of blocking with `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()`. Await asynchronous I/O directly rather than wrapping it in `Task.Run`; `Task.Run` still uses the same thread pool.

For heavy CPU-bound tests, use a shared [`ParallelLimiter<T>`](#parallellimitert--limiting-concurrent-test-count) to reduce concurrency for those tests. The limiter caps concurrent tests that share it, not the number of tasks each test creates.

## `[NotInParallel]` — Disabling Parallelism[​](#notinparallel--disabling-parallelism "Direct link to notinparallel--disabling-parallelism")

Add `[NotInParallel]` to prevent a test from running at the same time as other constrained tests.

It accepts an optional array of **constraint keys**. Tests that share any key will never overlap. Tests with no common key may still run concurrently.

If no keys are supplied, the test runs completely alone — no other test executes at the same time.

```
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

### Global `[NotInParallel]`[​](#global-notinparallel "Direct link to global-notinparallel")

Disable parallelism for every test in an assembly (run all tests sequentially):

```
[assembly: NotInParallel]
```

## `[ParallelGroup]` — Grouping Tests into Phases[​](#parallelgroup--grouping-tests-into-phases "Direct link to parallelgroup--grouping-tests-into-phases")

`[ParallelGroup("key")]` batches classes into groups. Tests within the same group run in parallel with each other, but no tests from other groups run at the same time. The engine finishes one group entirely before starting the next.

```
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

## `[ParallelLimiter<T>]` — Limiting Concurrent Test Count[​](#parallellimitert--limiting-concurrent-test-count "Direct link to parallellimitert--limiting-concurrent-test-count")

`[ParallelLimiter<T>]` caps how many tests sharing the same limiter type can run concurrently. The generic type argument must implement `IParallelLimit` with a public parameterless constructor.

The limit is shared across **all** tests referencing the same `IParallelLimit` type. Tests with a different limiter type or no limiter are unaffected.

An explicit `[ParallelLimiter<T>]` always takes precedence over a limiter set programmatically through `TestRegisteredContext.SetParallelLimiter`, including one supplied by an executor. This precedence does not depend on registration callback order. Without an explicit attribute, the last programmatic limiter applies.

```
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

### Assembly-Level Limiter[​](#assembly-level-limiter "Direct link to Assembly-Level Limiter")

```
[assembly: ParallelLimiter<MyAssemblyParallelLimit>]



public record MyAssemblyParallelLimit : IParallelLimit

{

    public int Limit => 2;

}
```

More specific attributes override less specific ones. Precedence: Method > Class > Assembly.

## Setting Maximum Parallel Tests[​](#setting-maximum-parallel-tests "Direct link to Setting Maximum Parallel Tests")

You can cap the total number of concurrent tests globally using the command line or an environment variable:

```
# Command-line flag

dotnet run --project MyTests -- --maximum-parallel-tests 4



# Environment variable

TUNIT_MAX_PARALLEL_TESTS=4 dotnet run --project MyTests
```

You can also set this programmatically via `context.Settings.Parallelism.MaximumParallelTests` in a `[Before(HookType.TestDiscovery)]` hook. See the [Programmatic Configuration](/docs/reference/programmatic-configuration.md) reference for details.

## When to Use Which[​](#when-to-use-which "Direct link to When to Use Which")

| Scenario                                                                               | Attribute                   |
| -------------------------------------------------------------------------------------- | --------------------------- |
| Tests must never overlap (e.g., shared database state)                                 | `[NotInParallel("key")]`    |
| Groups of tests can overlap internally but must be isolated from other groups          | `[ParallelGroup("key")]`    |
| Tests can overlap but you need to cap concurrency (e.g., limited external connections) | `[ParallelLimiter<T>]`      |
| Disable all parallelism for an assembly                                                | `[assembly: NotInParallel]` |

## Caveats[​](#caveats "Direct link to Caveats")

* If a test uses `[DependsOn(nameof(OtherTest))]` and the dependency has a different parallel limiter or group, ordering is not guaranteed.
* `[NotInParallel]` without keys is the most restrictive — the test runs completely alone. Use constraint keys when possible to allow unrelated tests to proceed.
