namespace TUnit.Core.SourceGenerator.Tests;

internal class GenericDiscoveryFailureFilterTests : TestsBase
{
    [Test]
    public Task Failure_Entries_Preserve_Filter_Attributes() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7437", "Tests.cs"), async generatedFiles =>
    {
        var failures = generatedFiles.Where(f => f.Contains("GenericConstraintViolation")).ToArray();
        await Assert.That(failures.Length).IsEqualTo(3);
        foreach (var failure in failures)
        {
            await Assert.That(failure).Contains("Issue7437Method");
            await Assert.That(failure).Contains("Issue7437Class");
            await Assert.That(failure).Contains("Issue7437Base");
            await Assert.That(failure).Contains("Issue7437Scope");
        }
    });
}
