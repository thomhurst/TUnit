namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritedGenericArgumentsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7490", "Tests.cs"),
        _ => Task.CompletedTask);
}
