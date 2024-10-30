using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class SomethingElseAttribute : Attribute, ITestStartEventReceiver
{
    public ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        return ValueTask.CompletedTask;
    }
}