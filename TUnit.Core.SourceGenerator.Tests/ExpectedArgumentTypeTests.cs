
namespace TUnit.Core.SourceGenerator.Tests;

internal class ExpectedArgumentTypeTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ExpectedArgumentTypeTests.cs"), _ => Task.CompletedTask);
}
