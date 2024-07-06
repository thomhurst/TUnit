using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class SomethingElseAttribute : Attribute, IBeforeTestAttribute
{
    public Task OnBeforeTest(TestContext testContext)
    {
        return Task.CompletedTask;
    }
}