namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassArgumentParameterTypesTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "Bugs", "_7440", "ClassArgumentParameterTypesTests.cs"),
        _ => Task.CompletedTask);
}
