namespace TUnit.Core.SourceGenerator.Tests;

internal class TypedArrayArgumentsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7399", "TypedArrayArgumentsTests.cs"),
        _ => Task.CompletedTask);
}
