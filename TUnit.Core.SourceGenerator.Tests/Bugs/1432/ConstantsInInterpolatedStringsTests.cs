
namespace TUnit.Core.SourceGenerator.Tests.Bugs._1432;

internal class ConstantsInInterpolatedStringsTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1432",
            "ConstantsInInterpolatedStringsTests.cs"),
        async generatedFiles =>
        {
            });
}
