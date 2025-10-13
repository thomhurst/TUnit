using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class MethodAssertionGeneratorTests : TestsBase<MethodAssertionGenerator>
{
    [Test]
    public Task BoolMethod() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "BoolMethodAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsPositive_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("Int_IsPositive_Assertion");
            await Assert.That(mainFile!).Contains("Int_IsGreaterThan_Int_Assertion");
            await Assert.That(mainFile!).Contains("public static Int_IsPositive_Assertion IsPositive");
        });

    [Test]
    public Task AssertionResultMethod() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AssertionResultMethodAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsEven_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("Int_IsEven_Assertion");
            await Assert.That(mainFile!).Contains("Int_IsBetween_Int_Int_Assertion");
            await Assert.That(mainFile!).Contains("return Task.FromResult(value.IsEven())"); // AssertionResult wrapped in Task
        });

    [Test]
    public Task AsyncBoolMethod() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AsyncBoolAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsPositiveAsync_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("IsPositiveAsync_Assertion");
            await Assert.That(mainFile!).Contains("var result = await"); // Awaits Task<bool>
        });

    [Test]
    public Task AsyncAssertionResultMethod() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AsyncAssertionResultAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsEvenAsync_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("IsEvenAsync_Assertion");
            await Assert.That(mainFile!).Contains("return await"); // Awaits and returns
        });
}
