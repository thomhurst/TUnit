using TUnit.Core;
using TUnit.Engine.Building;

namespace TUnit.UnitTests;

public class ClassRowCleanupTests
{
    [Test]
    public async Task ClaimedRowDoesNotTransferCleanupToLaterSkippedTest()
    {
        var original = CreateContext();
        var disposals = 0;
        original.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, original);
        var runnable = CreateContext();
        await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must use saved row"), runnable);

        var skipped = CreateContext();
        row.TransferCleanupTo(skipped);
        await DisposeRow(skipped);
        await Assert.That(disposals).IsEqualTo(0);
        await DisposeRow(runnable);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    public async Task UnclaimedRowTransfersCleanupToSkippedTest()
    {
        var original = CreateContext();
        var disposals = 0;
        original.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, original);
        var skipped = CreateContext();
        row.TransferCleanupTo(skipped);
        row.Clear();
        await DisposeRow(skipped);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    public async Task LentRowTransfersCleanupEvenWhenNextFactoryThrows()
    {
        var original = CreateContext();
        var disposals = 0;
        original.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, original);
        row.LendToTemporaryInstance();
        var failed = CreateContext();
        await Assert.That(async () => await row.TakeOrFetchAsync(
                () => throw new InvalidOperationException("Factory failure"), failed))
            .Throws<InvalidOperationException>();

        await DisposeRow(failed);
        await Assert.That(disposals).IsEqualTo(1);
        var later = CreateContext();
        row.TransferCleanupTo(later);
        await DisposeRow(later);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task EnumerationFailureOnlyDisposesUnclaimedClassScope(bool claimed)
    {
        var enumeration = CreateContext();
        var disposals = 0;
        enumeration.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, enumeration);
        var runnable = CreateContext();
        if (claimed)
        {
            await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must use saved row"), runnable);
        }

        // MoveNext throws after yielding a row (or before yielding any rows).
        var failed = row.CreateEnumerationFailureContext(enumeration);
        await DisposeRow(failed);
        await Assert.That(disposals).IsEqualTo(claimed ? 0 : 1);
        await DisposeRow(runnable);
        await Assert.That(disposals).IsEqualTo(1);
    }

    private static TestBuilderContext CreateContext() => new()
    {
        TestMetadata = TestContext.Current!.TestDetails.MethodMetadata
    };

    private static async Task DisposeRow(TestBuilderContext context)
    {
        if (context.EventsIfCreated?.OnDataRowDispose is { } cleanup)
        {
            foreach (var handler in cleanup.InvocationList)
            {
                await handler.InvokeAsync(TestContext.Current!, (TestContext.Current!, null));
            }
        }
    }
}
