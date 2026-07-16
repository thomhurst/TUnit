using System.Diagnostics;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TestContextTests
{
    [Test]
    public async Task Test()
    {
        var id = TestContext.Current!.Id;

        var context = TestContext.GetById(id);

        await Assert.That(context).IsNotNull();
    }

#if NET
    [Test]
    public async Task TestContext_Resolves_From_Activity_Baggage_When_AsyncLocal_Is_Null()
    {
        var context = TestContext.Current!;
        var testId = context.Id;

        // Suppress execution context flow to simulate a server thread pool
        // where AsyncLocal<TestContext> was never set.
        // Undo() must happen on the same thread as SuppressFlow(), so
        // launch the task inside the suppressed scope but await it outside.
        Task<TestContext?> task;
        var flowControl = ExecutionContext.SuppressFlow();
        try
        {
            task = Task.Run(() =>
            {
                // Simulate what OTel does on the receiving side:
                // create a new Activity with baggage extracted from W3C headers
                const string sourceName = "test-otel-simulation";
                using var listener = new ActivityListener
                {
                    ShouldListenTo = source => source.Name == sourceName,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                        ActivitySamplingResult.AllDataAndRecorded
                };
                ActivitySource.AddActivityListener(listener);

                using var source = new ActivitySource(sourceName);
                using var activity = source.StartActivity("incoming-request");
                activity?.SetBaggage(TUnitActivitySource.TagTestId, testId);

                return TestContext.Current;
            });
        }
        finally
        {
            flowControl.Undo();
        }

        var resolved = await task;
        await Assert.That(resolved).IsSameReferenceAs(context);
    }
#endif
}
