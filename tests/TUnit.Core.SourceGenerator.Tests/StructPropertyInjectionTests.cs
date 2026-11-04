namespace TUnit.Core.SourceGenerator.Tests;

internal class StructPropertyInjectionTests : TestsBase
{
    [Test]
    public Task Test() => PropertyInjectionGenerator.RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "Bugs", "_7609",
            "StructPropertyInjectionTests.cs"), _ => Task.CompletedTask);
}
