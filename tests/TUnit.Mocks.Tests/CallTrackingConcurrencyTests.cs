using TUnit.Assertions.Enums;
using TUnit.Mocks.Setup;

namespace TUnit.Mocks.Tests;

public class CallTrackingConcurrencyTests
{
    [Test]
    public async Task History_Growth_Preserves_The_First_Record()
    {
        var engine = new MockEngine<object>(MockBehavior.Loose);
        engine.HandleCall(0, "Call", 1);

        var first = engine.GetCallsFor(0).Single();
        var arguments = first.Arguments;

        for (var value = 2; value <= 10; value++)
        {
            engine.HandleCall(0, "Call", value);
        }

        var history = engine.GetCallsFor(0);
        await Assert.That(history.Count).IsEqualTo(10);
        await Assert.That(history[0]).IsSameReferenceAs(first);
        await Assert.That(history[0].Arguments).IsSameReferenceAs(arguments);
        await Assert.That(history.Select(call => (int)call.Arguments[0]!))
            .IsEquivalentTo(Enumerable.Range(1, 10), CollectionOrdering.Matching);
    }

    [Test]
    public async Task Published_Counts_Match_History_After_Concurrent_Growth_And_Clear()
    {
        var engine = new MockEngine<object>(MockBehavior.Loose);

        for (var pass = 0; pass < 2; pass++)
        {
            await Task.WhenAll(Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
            {
                var memberId = worker % 2 == 0 ? 0 : 15;
                for (var call = 0; call < 512; call++)
                {
                    engine.HandleCall(memberId, "Call", []);
                }
            })));

            await Assert.That(engine.GetCallCountFor(0)).IsEqualTo(2048);
            await Assert.That(engine.GetCallCountFor(15)).IsEqualTo(2048);
            await Assert.That(engine.GetCallCountFor(7)).IsEqualTo(0);
            await Assert.That(engine.GetAllCalls().Count).IsEqualTo(4096);

            engine.ClearCalls();
            await Assert.That(engine.GetCallCountFor(0)).IsEqualTo(0);
            await Assert.That(engine.GetCallCountFor(15)).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Behavior_And_Rare_State_Mutations_Share_Their_Synchronization()
    {
        var setup = new MethodSetup(0, [], "Call");
        var builder = new MethodSetupBuilder<int>(setup);
        builder.Returns(42);
        var callbackCount = 0;

        var callbacks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 16; i++)
            {
                builder.Callback(() => Interlocked.Increment(ref callbackCount));
            }
        }));
        var assignments = Task.Run(() =>
        {
            for (var i = 0; i < 64; i++)
            {
                setup.SetOutRefValue(i, i);
                setup.AddEventRaise(new EventRaiseInfo("Changed", i));
            }
        });
        await Task.WhenAll(callbacks.Append(assignments));

        var result = setup.GetNextBehavior()!.Execute([]);

        await Assert.That(result).IsEqualTo(42);
        await Assert.That(callbackCount).IsEqualTo(128);
        await Assert.That(setup.OutRefAssignments!.Count).IsEqualTo(64);
        await Assert.That(setup.GetEventRaises().Count).IsEqualTo(64);
    }
}
