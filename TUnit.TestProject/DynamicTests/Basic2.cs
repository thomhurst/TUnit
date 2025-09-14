using TUnit.TestProject.Attributes;

#pragma warning disable TUnitWIP0001

namespace TUnit.TestProject.DynamicTests;

[EngineTest(ExpectedResult.Pass)]
public class Basic2
{
    [DynamicTestBuilder]
    public void TestStateMachine(DynamicTestBuilderContext context)
    {
        var machine = new StateMachine();

        context.AddTest(
            new DynamicTest<Basic2>
            {
                TestMethod = @class => @class.AssertNotStarted(DynamicTestHelper.Argument<string>()),
                TestMethodArguments = [machine.CurrentState],
                Attributes = []
            }
        );

        machine.Advance();

        context.AddTest( // 👈 Problem: if first test fails, this one doesn't run?
            new DynamicTest<Basic2>
            {
                TestMethod = @class => @class.AssertQueuedAfterAdvance(DynamicTestHelper.Argument<string>()), // 👈 Problem: this needs to expect a Task
                TestMethodArguments = [machine.CurrentState],
                Attributes = []
            }
        );
    }

    public async Task AssertNotStarted(string currentState)
    {
        await Assert.That(currentState).IsEqualTo("not_started");
    }

    public async Task AssertQueuedAfterAdvance(string currentState)
    {
        await Assert.That(currentState).IsEqualTo("queued");
    }
}

/// <summary>
/// Simple state machine
/// </summary>
public class StateMachine
{
    public string CurrentState { get; set; } = "not_started";

    public void Advance()
    {
        CurrentState = CurrentState switch
        {
            "not_started" => "queued",
            "queued" => "starting",
            "starting" => "running",
            "running" => "completed",
            _ => "not_started"
        };
    }
}
