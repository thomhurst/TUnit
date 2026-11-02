namespace TUnit.Core.SourceGenerator.Tests;

internal class UninferredClassDataSourceTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject", "Bugs", "_7355", "Tests.cs"),
        async generatedFiles =>
        {
            var failures = generatedFiles.Where(f => f.Contains("cannot be inferred at compile time")).ToArray();
            await Assert.That(failures.Length).IsEqualTo(2);
            var failure = failures.Single(f => f.Contains("InheritedUntypedClassSource"));
            await Assert.That(failure).Contains("RegisterGenericConstraintViolations");
            await Assert.That(failure).Contains("Use [GenerateGenericTest]");
            await Assert.That(failure).Contains("or a typed class data source");
            await Assert.That(generatedFiles.Count(f => f.Contains("RegisterEntries<"))).IsEqualTo(2);
        });
}
