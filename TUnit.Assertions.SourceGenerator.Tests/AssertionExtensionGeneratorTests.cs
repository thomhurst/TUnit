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
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("IsEmpty");
        });

    [Test]
    public Task SingleGenericParameter() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "SingleGenericParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("IsNull");
            await Assert.That(generatedFiles[0]).Contains("<TValue>");
        });

    [Test]
    public Task MultipleGenericParameters() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MultipleGenericParametersAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("IsAssignableTo");
            await Assert.That(generatedFiles[0]).Contains("<TValue, TTarget>");
        });

    [Test]
    public Task AssertionWithOptionalParameter() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "OptionalParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("IsNotEqualTo");
            await Assert.That(generatedFiles[0]).Contains("= null");
        });

    [Test]
    public Task AssertionWithGenericConstraints() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "GenericConstraintsAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("IsGreaterThan");
            await Assert.That(generatedFiles[0]).Contains("where TValue : System.IComparable<TValue>");
        });

    [Test]
    public Task AssertionWithMultipleConstructors() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MultipleConstructorsAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            // Should generate multiple overloads
            var containsCount = System.Text.RegularExpressions.Regex.Matches(generatedFiles[0], "public static.*IsEqualTo").Count;
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
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("IsTrue");
            await Assert.That(generatedFiles[0]).Contains("IsFalse");
        });

    [Test]
    public Task AssertionWithDefaultValues() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "DefaultValuesAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("= true");
            await Assert.That(generatedFiles[0]).Contains("= 0");
            await Assert.That(generatedFiles[0]).Contains("= \"default\"");
        });

    [Test]
    public Task AssertionWithEnumDefault() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "EnumDefaultAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("StringComparison.");
        });

    [Test]
    public Task AssertionWithMultipleParameters() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MultipleParametersAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().GreaterThanOrEqualTo(1);
            await Assert.That(generatedFiles[0]).Contains("IsBetween");
            // Should have CallerArgumentExpression for both parameters
            var callerExprCount = System.Text.RegularExpressions.Regex.Matches(generatedFiles[0], "CallerArgumentExpression").Count;
            await Assert.That(callerExprCount).IsGreaterThanOrEqualTo(2);
        });
}
