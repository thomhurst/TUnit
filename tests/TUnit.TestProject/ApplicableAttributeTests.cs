namespace TUnit.TestProject;

public class ApplicableAttributeTests
{
    [Test, CustomSkip, SomethingElse]
    public async Task Test()
    {
        await Task.CompletedTask;
    }
}
