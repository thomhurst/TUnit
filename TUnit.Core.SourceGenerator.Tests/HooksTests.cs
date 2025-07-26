
namespace TUnit.Core.SourceGenerator.Tests;

internal class HooksTests : TestsBase
{
    [Test]
    public Task NullableByteArgumentTests() => HooksGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NullableByteArgumentTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(0);
        });

    [Test]
    public Task DisposableFieldTests() => HooksGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DisposableFieldTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);
        });
}
