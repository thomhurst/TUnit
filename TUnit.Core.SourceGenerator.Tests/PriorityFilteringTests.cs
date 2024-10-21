using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class PriorityFilteringTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "PriorityFilteringTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = 
                [
                    Path.Combine(Git.RootDirectory.FullName,
                        "TUnit.TestProject",
                        "Enums",
                        "PriorityLevel.cs")
                ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(6);
        });
}