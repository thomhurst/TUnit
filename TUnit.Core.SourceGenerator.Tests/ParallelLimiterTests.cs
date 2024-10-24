using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

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
        async generatedFiles =>
        {
            await AssertFileContains(generatedFiles[0], "ParallelLimit = TUnit.Core.ParallelLimitProvider.GetParallelLimit<global::TUnit.TestProject.Dummy.ParallelLimit3>(),");
        });
}