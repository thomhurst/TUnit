namespace TUnit.TestProject;

public class CustomFilteringTests
{
    [Test, Property("one", "yes")]
    public async Task Custom_Filter_One()
    {
        await Task.CompletedTask;
    }

    [Test, Property("one", "no")]
    public async Task Custom_Filter_Two()
    {
        await Task.CompletedTask;
    }
}
