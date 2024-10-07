using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

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