using TUnit.Core;

namespace TUnit.UnitTests;

/// <summary>
/// A generic method's row that does not fit its type arguments is discarded after it was fetched against
/// the class row's events. Only the handlers that the discarded row added are detached and raised, and the
/// class row's handlers travel on with its objects (#7205).
/// </summary>
public class AsyncEventDetachTests
{
    [Test]
    public async Task Detaches_Only_The_Handlers_Added_Since_The_Snapshot_In_Order()
    {
        var raised = new List<string>();
        AsyncEvent<TestContext>? onDispose = null;
        onDispose += Record(raised, "class row");
        AsyncEvent<TestContext>.Invocation[] kept = [.. onDispose.InvocationList];

        onDispose += Record(raised, "method row 1");
        onDispose += Record(raised, "method row 2");

        var detached = onDispose.DetachAllExcept(kept);

        await Assert.That(detached).IsNotNull();
        foreach (var invocation in detached!)
        {
            await invocation.InvokeAsync(this, null!);
        }

        await Assert.That(raised).IsEquivalentTo(["method row 1", "method row 2"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(onDispose.InvocationList).IsEquivalentTo(kept);
    }

    [Test]
    public async Task Detaches_Every_Handler_When_Nothing_Was_Kept()
    {
        AsyncEvent<TestContext>? onDispose = null;
        onDispose += Record([], "method row");

        var detached = onDispose.DetachAllExcept(null);

        await Assert.That(detached).HasCount(1);
        await Assert.That(onDispose.InvocationList).IsEmpty();
    }

    [Test]
    public async Task Returns_Null_When_No_Handler_Was_Added()
    {
        AsyncEvent<TestContext>? onDispose = null;
        onDispose += Record([], "class row");
        AsyncEvent<TestContext>.Invocation[] kept = [.. onDispose.InvocationList];

        await Assert.That(onDispose.DetachAllExcept(kept)).IsNull();
        await Assert.That(onDispose.InvocationList).HasCount(1);
    }

    private static Func<object, TestContext, ValueTask> Record(List<string> raised, string name)
        => (_, _) =>
        {
            raised.Add(name);
            return default;
        };
}
