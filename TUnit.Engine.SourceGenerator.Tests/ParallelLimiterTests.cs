using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ParallelLimiterTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ParallelLimiterTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "Dummy",
                    "ParallelLimit3.cs")
            ] 
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(12));
            
            AssertFileContains(generatedFiles[0], "ParallelLimit = TUnit.Core.ParallelLimitProvider.GetParallelLimit<global::TUnit.TestProject.Dummy.ParallelLimit3>(),");
        });
}