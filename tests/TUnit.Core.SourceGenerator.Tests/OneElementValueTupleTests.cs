namespace TUnit.Core.SourceGenerator.Tests;

internal class OneElementValueTupleTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "OneElementValueTupleTests.cs"), _ => Task.CompletedTask);
}
