namespace TUnit.Core.SourceGenerator.Tests;

internal class GenericDiscoveryFailureFilterTests : TestsBase
{
    [Test]
    public Task Failure_Entries_Preserve_Filter_Attributes() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7437", "Tests.cs"), async generatedFiles =>
    {
        var failures = generatedFiles.Where(f => f.Contains("GenericConstraintViolation")).ToArray();
        await Assert.That(failures.Length).IsEqualTo(7);
        var inheritedFailure = failures.Single(f => f.Contains("InvalidInheritedClass"));
        await Assert.That(inheritedFailure).DoesNotContain("+NonInheritedCategoryAttribute");
        await Assert.That(inheritedFailure).DoesNotContain("), 1)");
        await Assert.That(inheritedFailure).Contains("), 0)");
        var scopedFailures = failures.Where(f => f.Contains("Issue7437Method")).ToArray();
        await Assert.That(scopedFailures.Length).IsEqualTo(3);
        foreach (var failure in scopedFailures)
        {
            await Assert.That(failure).Contains("Issue7437Method");
            await Assert.That(failure).Contains("Issue7437Class");
            await Assert.That(failure).Contains("Issue7437Base");
            await Assert.That(failure).Contains("Issue7437Scope");
        }
    });
}
