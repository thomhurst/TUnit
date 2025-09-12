using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class AssertionMethodGeneratorTests : TestsBase<AssertionMethodGenerator>
{
    [Test]
    public Task GeneratesCharAssertions() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Assertions",
            "CharAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles);
        });

    [Test]
    public Task GeneratesEnumAssertions() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Assertions",
            "EnumAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles);
        });

    [Test]
    public Task GeneratesPathAssertions() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Assertions",
            "PathAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles);
        });

    [Test]
    public Task GeneratesStringAssertions() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Assertions",
            "StringAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles);
        });

    [Test]
    public Task GeneratesUriAssertions() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions",
            "Assertions",
            "UriAssertionExtensions.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles);
        });
}
