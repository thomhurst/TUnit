namespace TUnit.Core.SourceGenerator.Tests;

internal class WholeTupleInferenceTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7638", "WholeTupleInferenceTests.cs"), _ => Task.CompletedTask);
}
