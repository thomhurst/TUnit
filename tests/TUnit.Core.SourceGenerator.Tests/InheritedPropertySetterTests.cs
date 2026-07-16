using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritedPropertySetterTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "InheritedPropertySetterTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.TestsDirectory.FullName,
                    "TUnit.TestProject",
                    "PropertySetterTests.cs")
            ]
        },
        async generatedFiles =>
        {
            });
}
