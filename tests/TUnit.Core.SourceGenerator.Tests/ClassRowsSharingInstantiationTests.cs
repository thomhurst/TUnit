namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassRowsSharingInstantiationTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7335", "ClassRowsSharingInstantiationTests.cs"),
        _ => Task.CompletedTask);
}
