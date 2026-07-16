using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassConstructorTest : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "ClassConstructorTest.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.TestsDirectory.FullName,
                    "TUnit.TestProject",
                    "DummyReferenceTypeClass.cs"),
                Path.Combine(Git.TestsDirectory.FullName,
                    "TUnit.TestProject",
                    "DependencyInjectionClassConstructor.cs")
            ]
        },
        async generatedFiles =>
        {
            });
}
