namespace TUnit.TestProject;

internal sealed class InternalTestWithArgumentsTest
{
    [Test]
    [Arguments("1", "2")]
    internal Task TestMethod(string s1, string s2) => Task.CompletedTask;
}
