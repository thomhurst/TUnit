using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class SomethingElseAttribute : Attribute, ITestStartEventReceiver
{
    public ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        return default;
    }

    public int Order => 0;
}