namespace TUnit.Engine.SourceGenerator.Tests;

public class NotInParallelTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NotInParallelTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(12));

            foreach (var generatedFile in generatedFiles)
            {
                Assert.That(generatedFile, Does.Contain("NotInParallelConstraintKeys = [],"));
            }
        });
}