namespace TUnit.Core.SourceGenerator.Tests;

internal class NestedTupleBindingTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7485", "NestedTupleBindingTests.cs"), _ => Task.CompletedTask);
}
