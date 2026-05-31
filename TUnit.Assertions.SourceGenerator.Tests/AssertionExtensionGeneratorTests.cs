using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsAssignableTo"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsAssignableTo");
            await Assert.That(extensionFile!).Contains("<TValue, TTarget>");
        });

    [Test]
    public Task ConcreteReceiverWithExtraGeneric() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "ConcreteReceiverWithExtraGenericAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("ConcreteReceiverWithExtraGenericMatches"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("ConcreteReceiverWithExtraGenericMatches");

            // The covariant receiver-type parameter (if applied) and the class's own type
            // parameter must appear in a single merged generic parameter list. Two adjacent
            // blocks (<TActual><T> or <X><Y>) is invalid C# syntax. The compile-clean check
            // below is the structural guard; this surface-level assertion pins the absence
            // of adjacent generic-parameter blocks regardless of whether covariance fired
            // for this fixture's compilation context.
            await Assert.That(extensionFile!).DoesNotContain("><");

            // Compile-clean gate: parse + compile the generated source through Roslyn and
            // assert no error-severity diagnostic. Catches the entire class of emit-syntax
            // bugs (mis-paired brackets, wrong default rendering, adjacent generic blocks)
            // regardless of specific diagnostic id.
            var trees = generatedFiles
                .Select(source => CSharpSyntaxTree.ParseText(source))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                "CompileCheck",
                trees,
                ReferencesHelper.References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // On the net472 leg of CI, the Polyfill assembly's CallerArgumentExpressionAttribute
            // is declared internal, which produces a CS0122 ('inaccessible due to its protection
            // level') false positive when Roslyn compiles the generator's output through the test
            // project's reference set. Filter CS0122 only on net472; the emit shape is still
            // pinned by the `.Net4_7` snapshot file. On modern TFMs the BCL attribute is public,
            // so CS0122 (if it ever appears) would be a genuine signal and is not filtered.
            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
#if NETFRAMEWORK
                .Where(d => !string.Equals(d.Id, "CS0122", StringComparison.Ordinal))
#endif
                .Select(d => $"{d.Id}: {d.GetMessage()}")
                .ToArray();

            await Assert.That(errors).IsEmpty();
        });

    [Test]
    public Task AssertionWithOptionalParameter() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "OptionalParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
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
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("IsBetween"));
            await Assert.That(extensionFile).IsNotNull();
            await Assert.That(extensionFile!).Contains("IsBetween");
            // Should have CallerArgumentExpression for both parameters
            var callerExprCount = System.Text.RegularExpressions.Regex.Matches(extensionFile!, "CallerArgumentExpression").Count;
            await Assert.That(callerExprCount).IsGreaterThanOrEqualTo(2);
        });

    [Test]
    public Task ValueTypeDefaultParameter() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AssertionExtensionValueTypeDefaultParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var extensionFile = generatedFiles.First();
            await Assert.That(extensionFile).IsNotNull();

            // A non-nullable value-type constructor parameter declared with `= default` must
            // render as the bare `default` literal, not `= null`. The literal `null` is invalid
            // for a non-nullable value type and produces CS1750. The trailing comma anchors the
            // assertion to bare `default`, ruling out the longer `default(TypeName)` form.
            await Assert.That(extensionFile).Contains("CancellationToken token = default,");
            await Assert.That(extensionFile).DoesNotContain("CancellationToken token = null");

            await CompileChecker.AssertNoErrors(generatedFiles);
        });
}
