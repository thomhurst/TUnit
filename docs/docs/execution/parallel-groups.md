# Parallel Groups

Parallel groups control which tests can run at the same time. Classes that share the same `[ParallelGroup("key")]` are batched together: tests within the same group run in parallel with each other, but no tests from other groups run alongside them. The engine finishes one group entirely before starting the next.

## How It Works

Consider two groups:

- **Group A**: `ClassA1` and `ClassA2` both have `[ParallelGroup("GroupA")]`
- **Group B**: `ClassB1` has `[ParallelGroup("GroupB")]`

At runtime:
1. All tests from `ClassA1` and `ClassA2` run in parallel with each other. No other tests execute during this phase.
2. Once every test in Group A finishes, all tests from `ClassB1` run. No other tests execute during this phase.

Tests that do not belong to any parallel group run separately, following the normal parallel execution rules.

## Example

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

`UserRepositoryTests` and `OrderRepositoryTests` share `"Database"`, so their tests all run in parallel with each other. `PaymentApiTests` is in `"ExternalApi"`, so it runs in a separate phase -- after the `"Database"` group finishes (or before, depending on scheduling order).

## Difference from `[NotInParallel]`

`[NotInParallel]` prevents individual tests from running at the same time as other tests that share the same constraint key. Each test with `[NotInParallel("Database")]` runs one at a time, sequentially.

`[ParallelGroup("Database")]` allows tests within the group to run in parallel with each other. Only tests from *other* groups are excluded during that phase.

Use `[NotInParallel]` when tests must never overlap. Use `[ParallelGroup]` when tests can safely overlap with each other but must be isolated from unrelated tests.
