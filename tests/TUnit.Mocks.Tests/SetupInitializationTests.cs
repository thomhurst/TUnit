using TUnit.Mocks.Setup;
using TUnit.Mocks.Setup.Behaviors;

namespace TUnit.Mocks.Tests;

public class SetupInitializationTests
{
    [Test]
    public async Task Concurrent_First_Behaviors_Compose_Without_Losing_Callbacks()
    {
        for (var pass = 0; pass < 32; pass++)
        {
            var setup = new MethodSetup(0, [], "Call");
            var count = 0;
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
            {
                await start.Task;
                setup.AddBehavior(new CallbackBehavior(() => Interlocked.Increment(ref count)));
            })).ToArray();

            start.SetResult();
            await Task.WhenAll(tasks);
            setup.GetNextBehavior()!.Execute([]);

            await Assert.That(count).IsEqualTo(8);
        }
    }

    [Test]
    public async Task First_Behavior_Racing_With_Then_Preserves_A_Valid_Sequence()
    {
        for (var pass = 0; pass < 128; pass++)
        {
            var setup = new MethodSetup(0, [], "Call");
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var add = Task.Run(async () =>
            {
                await start.Task;
                setup.AddBehavior(new ReturnBehavior<int>(1));
            });
            var then = Task.Run(async () =>
            {
                await start.Task;
                setup.Then();
            });
            start.SetResult();
            await Task.WhenAll(add, then);
            setup.AddBehavior(new ReturnBehavior<int>(2));

            // Then either precedes the first return (one composite step) or follows it (two steps).
            var first = (int)setup.GetNextBehavior()!.Execute([])!;
            await Assert.That(first is 1 or 2).IsTrue();
            await Assert.That(setup.GetNextBehavior()!.Execute([])).IsEqualTo(2);
            await Assert.That(setup.GetNextBehavior()!.Execute([])).IsEqualTo(2);
        }
    }

    [Test]
    public async Task Configured_Members_Record_All_Concurrent_First_Calls_Across_Buffer_Growth()
    {
        var engine = new MockEngine<object>(MockBehavior.Loose);
        for (var member = 0; member < 32; member++)
        {
            var setup = new MethodSetup(member, [], "Call");
            new MethodSetupBuilder<int>(setup).Returns(member);
            engine.AddSetup(setup);
        }
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(async () =>
        {
            await start.Task;
            for (var call = 0; call < 512; call++)
            {
                var member = worker * 4 + call % 4;
                var result = engine.HandleCallWithReturn(member, "Call", [], -1);
                if (result != member)
                {
                    throw new InvalidOperationException("Configured return was lost.");
                }
            }
        })).ToArray();
        start.SetResult();
        await Task.WhenAll(tasks);

        await Assert.That(engine.GetAllCalls().Count).IsEqualTo(4096);
        for (var member = 0; member < 32; member++)
        {
            await Assert.That(engine.GetCallCountFor(member)).IsEqualTo(128);
        }
    }
}
