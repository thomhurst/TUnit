using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class AutoDataTests
{
    [AutoData]
    [Test]
    public Task Test1(string value1, int value2, double value3, bool value4)
    {
        return Task.CompletedTask;
    }
}