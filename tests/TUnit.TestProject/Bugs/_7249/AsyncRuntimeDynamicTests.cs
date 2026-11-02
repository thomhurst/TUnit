using TUnit.Core.Extensions;

#pragma warning disable WIP

namespace TUnit.TestProject.Bugs._7249;

// A dynamic test added with AddDynamicTest must await the Task or ValueTask its method returns,
// so a failure after the first await fails the test (#7249). Run only by
// TUnit.Engine.Tests/Bug7249AsyncRuntimeDynamicTests: it contains intentional failures.

// Not a test class: only used as the class of the dynamic tests.
public sealed class AsyncRuntimeDynamicConsumer
{
    public async Task Task_Fails_After_Await()
    {
        await Task.Yield();
        throw new InvalidOperationException("Task dynamic test failed after await");
    }

    public async ValueTask ValueTask_Fails_After_Await()
    {
        await Task.Yield();
        throw new InvalidOperationException("ValueTask dynamic test failed after await");
    }

    public async Task Task_Passes()
    {
        await Task.Yield();
    }

    public void Sync_Passes()
    {
    }
}

public class AsyncRuntimeDynamicTests
{
    [Test]
    public async Task Adds_Dynamic_Tests()
    {
        var context = TestContext.Current!;

        await context.AddDynamicTest(new DynamicTest<AsyncRuntimeDynamicConsumer>
        {
            TestMethod = consumer => consumer.Task_Fails_After_Await(),
            Attributes = []
        });

        await context.AddDynamicTest(new DynamicTest<AsyncRuntimeDynamicConsumer>
        {
            TestMethod = consumer => consumer.ValueTask_Fails_After_Await(),
            Attributes = []
        });

        await context.AddDynamicTest(new DynamicTest<AsyncRuntimeDynamicConsumer>
        {
            TestMethod = consumer => consumer.Task_Passes(),
            Attributes = []
        });

        await context.AddDynamicTest(new DynamicTest<AsyncRuntimeDynamicConsumer>
        {
            TestMethod = consumer => consumer.Sync_Passes(),
            Attributes = []
        });
    }
}
