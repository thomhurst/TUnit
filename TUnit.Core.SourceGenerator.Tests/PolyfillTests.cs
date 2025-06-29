using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

#if !NET
internal class PolyfillTests : TestsBase
{
    [Test]
    public Task Test_Without_BuildProperty_WithoutType() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsGreaterThan(0);
        });

    [Test]
    public Task Test_With_False_BuildProperty() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        new RunTestOptions
        {
            BuildProperties = new Dictionary<string, string>
            {
                ["build_property.EnableTUnitPolyfills"] = "false",
            }
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsEmpty();
        });
}
#endif
