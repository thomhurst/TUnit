using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class AssertionMethodGeneratorTests : TestsBase<AssertionMethodGenerator>
{
    [Test]
    public Task GeneratesCharIsDigitAssertion() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesCharIsDigitAssertion.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesMultipleAssertionTypes() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesMultipleAssertionTypes.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesMultipleAssertions() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesMultipleAssertions.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesWithCustomMethodName() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesWithCustomMethodName.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesWithMultipleParameters() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesWithMultipleParameters.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesWithTwoParameters() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesWithTwoParameters.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesEnumIsDefinedAssertion() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesEnumIsDefinedAssertion.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesStringMethodsAssertion() => RunTest(Path.Combine(Sourcy.Git.RootDirectory.FullName,
        "TUnit.Assertions.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesStringMethodsAssertion.cs"),
        async generatedFiles =>
        {
            await Verify(string.Join("\n---\n", generatedFiles));
        });
}