namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7197 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7197",
        "NullableValueTypeGenericMethodTests.cs"),
        _ => Task.CompletedTask);
}
