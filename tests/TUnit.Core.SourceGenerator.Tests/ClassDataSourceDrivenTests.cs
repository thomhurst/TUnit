using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.TestsDirectory.FullName,
                    "TUnit.TestProject.Library",
                    "Models",
                    "SomeAsyncDisposableClass.cs")
            ]
        },
        async generatedFiles =>
        {
            });
}
