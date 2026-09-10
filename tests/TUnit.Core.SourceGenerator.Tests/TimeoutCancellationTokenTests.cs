
namespace TUnit.Core.SourceGenerator.Tests;

internal class TimeoutCancellationTokenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "TimeoutCancellationTokenTests.cs"),
        async generatedFiles =>
        {
            });
}
