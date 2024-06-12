using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class SomethingElseAttribute : Attribute, IApplicableTestAttribute
{
    public Task Apply(TestContext testContext)
    {
        return Task.CompletedTask;
    }
}