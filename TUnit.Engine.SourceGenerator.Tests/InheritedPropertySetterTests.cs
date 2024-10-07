using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class InheritedPropertySetterTests : TestsBase<InheritsTestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "InheritedPropertySetterTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "PropertySetterTests.cs")
            ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
        });
}