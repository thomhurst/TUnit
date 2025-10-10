namespace TUnit.TestProject;

internal sealed class InternalMethodWithArgumentsTests
{
    [Test]
    [Arguments("1", "2")]
    internal Task TestMethod(string s1, string s2)
    {
        return Task.CompletedTask;
    }

    [Test]
    [Arguments(1, 2)]
    [Arguments(3, 4)]
    internal void TestMethod_Multiple(int a, int b)
    {
    }

    [Test]
    internal void TestMethod_NoArguments()
    {
        // This should work regardless, but included for completeness
    }

    [Test]
    [Arguments("test")]
    public Task PublicMethod_WithArguments(string value)
    {
        // This should continue to work as before
        return Task.CompletedTask;
    }
}
