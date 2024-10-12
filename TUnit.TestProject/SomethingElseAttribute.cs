using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class SomethingElseAttribute : Attribute, ITestStartEvent
{
    public ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        return ValueTask.CompletedTask;
    }
}