namespace TUnit.Core.SourceGenerator.Tests;

internal class SoleTupleInferenceTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7639", "SoleTupleInferenceTests.cs"), _ => Task.CompletedTask);
}
