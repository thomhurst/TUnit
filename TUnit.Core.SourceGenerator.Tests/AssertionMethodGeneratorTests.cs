using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AssertionMethodGeneratorTests : TestsBase<AssertionMethodGenerator>
{
    [Test]
    public Task GeneratesCharIsDigitAssertion() => RunTest(Path.Combine(Git.RootDirectory.FullName,
        "TUnit.Core.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesCharIsDigitAssertion.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesMultipleAssertionTypes() => RunTest(Path.Combine(Git.RootDirectory.FullName,
        "TUnit.Core.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesMultipleAssertionTypes.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesMultipleAssertions() => RunTest(Path.Combine(Git.RootDirectory.FullName,
        "TUnit.Core.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesMultipleAssertions.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesWithCustomMethodName() => RunTest(Path.Combine(Git.RootDirectory.FullName,
        "TUnit.Core.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesWithCustomMethodName.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesWithMultipleParameters() => RunTest(Path.Combine(Git.RootDirectory.FullName,
        "TUnit.Core.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesWithMultipleParameters.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });

    [Test]
    public Task GeneratesWithTwoParameters() => RunTest(Path.Combine(Git.RootDirectory.FullName,
        "TUnit.Core.SourceGenerator.Tests",
        "AssertionMethodGeneratorTests.GeneratesWithTwoParameters.cs"),
        async generatedFiles =>
        {
            await Verify(generatedFiles[0]);
        });
}