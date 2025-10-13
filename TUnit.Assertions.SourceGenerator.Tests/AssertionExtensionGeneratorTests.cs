using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class AssertionExtensionGeneratorTests : TestsBase<AssertionExtensionGenerator>
{
    [Test]
    public Task NonGenericAssertion() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "NonGenericAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsEmpty"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsEmpty");
        });

    [Test]
    public Task SingleGenericParameter() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "SingleGenericParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsNull"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsNull");
            await Assert.That(extensionFile!).Contains("<TValue>");
        });

    [Test]
    public Task MultipleGenericParameters() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MultipleGenericParametersAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsAssignableTo"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsAssignableTo");
            await Assert.That(extensionFile!).Contains("<TValue, TTarget>");
        });

    [Test]
    public Task AssertionWithOptionalParameter() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "OptionalParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsNotEqualTo"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsNotEqualTo");
            await Assert.That(extensionFile!).Contains("= null");
        });

    [Test]
    public Task AssertionWithGenericConstraints() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "GenericConstraintsAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsGreaterThan"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsGreaterThan");
            await Assert.That(extensionFile!).Contains("where TValue : System.IComparable<TValue>");
        });

    [Test]
    public Task AssertionWithMultipleConstructors() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MultipleConstructorsAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsEqualTo"));
            await Assert.That(extensionFile).IsNotNull();
            // Should generate multiple overloads
            var containsCount = System.Text.RegularExpressions.Regex.Matches(extensionFile!, "public static.*IsEqualTo").Count;
            await Assert.That(containsCount).IsGreaterThan(1);
        });

    [Test]
    public Task AssertionWithNegatedMethod() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "NegatedMethodAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsTrue"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsTrue");
            await Assert.That(extensionFile!).Contains("IsFalse");
        });

    [Test]
    public Task AssertionWithDefaultValues() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "DefaultValuesAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("= true"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("= true");
            await Assert.That(extensionFile!).Contains("= 0");
            await Assert.That(extensionFile!).Contains("= \"default\"");
        });

    [Test]
    public Task AssertionWithEnumDefault() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "EnumDefaultAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("StringComparison"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("StringComparison.");
        });

    [Test]
    public Task AssertionWithMultipleParameters() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MultipleParametersAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsBetween"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsBetween");
            // Should have CallerArgumentExpression for both parameters
            var callerExprCount = System.Text.RegularExpressions.Regex.Matches(extensionFile!, "CallerArgumentExpression").Count;
            await Assert.That(callerExprCount).IsGreaterThanOrEqualTo(2);
        });
}
