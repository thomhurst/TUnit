namespace TUnit.Core.SourceGenerator.Tests;

internal class TupleInferenceTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7629", "TupleInferenceTests.cs"), _ => Task.CompletedTask);
}
