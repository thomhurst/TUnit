namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7782 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7782",
        "CustomDataSourceRowSettingsTests.cs"),
        _ => Task.CompletedTask);
}
