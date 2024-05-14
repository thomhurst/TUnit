using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class NotInParallelTests : TestsBase<TestsGenerator>
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