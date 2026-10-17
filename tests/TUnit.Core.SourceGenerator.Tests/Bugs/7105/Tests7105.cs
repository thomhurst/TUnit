namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Generic-method instantiations are named with C# keyword aliases and no namespaces
/// (<c>Generated&lt;List&lt;int&gt;&gt;</c>), as reflection mode names them (#7105).
/// </summary>
internal class Tests7105 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7105",
        "GenericNameParityTests.cs"),
        _ => Task.CompletedTask);
}
