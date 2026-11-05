namespace TUnit.Core.SourceGenerator.Tests;

internal class TupleOptionalParameterTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7212", "TupleParameterWithOptionalParameterTests.cs"), _ => Task.CompletedTask);
}
