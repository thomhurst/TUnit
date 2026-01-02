using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class AssertOverloadsGeneratorTests : TestsBase<AssertOverloadsGenerator>
{
    [Test]
    public Task BasicOverloadGeneration() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "SimpleAssertOverloadsTest.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);

            // Verify the generator produces wrapper types
            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).Contains("FuncString");
            await Assert.That(mainFile).Contains("AsyncFuncString");
            await Assert.That(mainFile).Contains("TaskString");
            await Assert.That(mainFile).Contains("ValueTaskString");
            await Assert.That(mainFile).Contains("IAssertionSource<string?>");

            // Verify overloads are generated
            await Assert.That(mainFile).Contains("public static FuncString That(");
            await Assert.That(mainFile).Contains("Func<string?> func");
            await Assert.That(mainFile).Contains("[OverloadResolutionPriority(3)]");
        });
}
