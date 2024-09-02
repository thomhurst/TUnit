using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class SomethingElseAttribute : Attribute, IBeforeTestAttribute
{
    public Task OnBeforeTest(BeforeTestContext context)
    {
        return Task.CompletedTask;
    }
}