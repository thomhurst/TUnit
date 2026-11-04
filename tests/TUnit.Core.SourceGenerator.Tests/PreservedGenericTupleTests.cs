namespace TUnit.Core.SourceGenerator.Tests;

internal class PreservedGenericTupleTests : TestsBase
{
    [Test]
    public Task Preserved_Tuples() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7514", "PreservedGenericTupleTests.cs"), _ => Task.CompletedTask);
}
