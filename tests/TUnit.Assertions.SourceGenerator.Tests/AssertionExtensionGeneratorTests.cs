using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Assertions.SourceGenerator.Tests.Options;
using TUnit.Tests.Shared;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class AssertionExtensionGeneratorTests : TestsBase<AssertionExtensionGenerator>
{
    [Test]
    public Task NonGenericAssertion() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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

            // Issue #5922: for a covariance-candidate receiver that also declares its own generic
            // parameter, the generator emits BOTH the covariant overload (keeps subclass binding)
            // and an inference-friendly pinned-receiver overload that omits the covariant TActual,
            // so the common exact-type call site only needs the class's own type argument.
            // Covariant overload: <TActual, T> receiver IAssertionSource<TActual>.
            await Assert.That(extensionFile!).Contains("ConcreteReceiverWithExtraGenericMatches<TActual, T>(this IAssertionSource<TActual> source");
            // Pinned overload: <T> only, receiver pinned to the concrete IAssertionSource<System.Exception>.
            await Assert.That(extensionFile!).Contains("ConcreteReceiverWithExtraGenericMatches<T>(this IAssertionSource<System.Exception> source");

            // Compile-clean gate: parse + compile the generated source through Roslyn and
            // assert no error-severity diagnostic. Catches the entire class of emit-syntax
            // bugs (mis-paired brackets, wrong default rendering, adjacent generic blocks)
            // regardless of specific diagnostic id.
            var trees = generatedFiles
                .Select(source => GeneratorTestInput.Parse(source))
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
    public Task ConcreteReceiverWithInferableGeneric() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "ConcreteReceiverWithInferableGenericAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);
            var extensionFile = generatedFiles.FirstOrDefault(f => f.Contains("ConcreteReceiverWithInferableGenericTagged"));
            await Assert.That(extensionFile).IsNotNull();

            // Issue #5922: the own type parameter T is inferable from the `T tag` value argument, so
            // the caller never names a type argument and the covariant overload binds on its own.
            // Covariant overload IS emitted (keeps subclass binding).
            await Assert.That(extensionFile!).Contains("ConcreteReceiverWithInferableGenericTagged<TActual, T>(this IAssertionSource<TActual> source");
            // The pinned-receiver overload would be pure dead weight here, so it must NOT be emitted.
            await Assert.That(extensionFile!).DoesNotContain("(this IAssertionSource<System.Exception> source");
        });

    [Test]
    public Task AssertionWithOptionalParameter() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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
        Path.Combine(Git.TestsDirectory.FullName,
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

    [Test]
    public Task DocumentedAssertionClasses() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "DocumentedAssertionExtension.cs"),
        async generatedFiles =>
        {
            // One file per assertion class.
            await Assert.That(generatedFiles).Count().IsEqualTo(7);

            var allFiles = string.Join(Environment.NewLine, generatedFiles);
            const string classPath = " path=\"/*[self::summary or self::remarks or self::typeparam]\"/>";
            const string constructorPath = " path=\"/*[(self::param and not(@name='context')) or self::exception]\"/>";
            const string rangeClass = "    /// <inheritdoc cref=\"T:TUnit.Assertions.Tests.TestData.DocumentedRangeAssertion\"" + classPath;

            // Edge 1: a documented class lends its docs first, then the documented constructor the
            // extension wraps lends its parameters (without the context) and exceptions.
            await Assert.That(allFiles).Contains(rangeClass + Environment.NewLine
                + "    /// <inheritdoc cref=\"M:TUnit.Assertions.Tests.TestData.DocumentedRangeAssertion.#ctor(TUnit.Assertions.Core.AssertionContext{System.Int32},System.Int32,System.Int32)\"" + constructorPath);

            // Edge 2: each constructor's extension names its own constructor.
            await Assert.That(allFiles).Contains(rangeClass + Environment.NewLine
                + "    /// <inheritdoc cref=\"M:TUnit.Assertions.Tests.TestData.DocumentedRangeAssertion.#ctor(TUnit.Assertions.Core.AssertionContext{System.Int32},System.Int32)\"" + constructorPath);

            // Edge 3: a generic class uses the ID syntax for type parameters, and the path names the
            // context parameter as the constructor does.
            await Assert.That(allFiles).Contains(
                "    /// <inheritdoc cref=\"T:TUnit.Assertions.Tests.TestData.DocumentedTagAssertion`1\"" + classPath + Environment.NewLine
                + "    /// <inheritdoc cref=\"M:TUnit.Assertions.Tests.TestData.DocumentedTagAssertion`1.#ctor(TUnit.Assertions.Core.AssertionContext{TUnit.Assertions.Tests.TestData.DocumentedSubject},`0)\" path=\"/*[(self::param and not(@name='assertionContext')) or self::exception]\"/>");

            // Edge 4: the pinned-receiver overload inherits the same docs as the covariant one.
            await Assert.That(System.Text.RegularExpressions.Regex.Matches(allFiles,
                System.Text.RegularExpressions.Regex.Escape("/// <inheritdoc cref=\"T:TUnit.Assertions.Tests.TestData.DocumentedTagOfTypeAssertion`1\"")).Count).IsEqualTo(2);

            // Edge 5: a documented class with an undocumented constructor lends only the class docs.
            await Assert.That(allFiles).Contains("    /// <inheritdoc cref=\"T:TUnit.Assertions.Tests.TestData.ClassOnlyDocumentedAssertion\"" + classPath);
            await Assert.That(allFiles).DoesNotContain("ClassOnlyDocumentedAssertion.#ctor");

            // Edge 6: an undocumented class keeps the placeholder, even with a documented constructor,
            // because the class's docs are what describe the assertion.
            await Assert.That(allFiles).Contains("    /// Extension method for UndocumentedAssertion.");
            await Assert.That(allFiles).Contains("    /// Extension method for ConstructorOnlyDocumentedAssertion.");
            await Assert.That(allFiles).DoesNotContain("ConstructorOnlyDocumentedAssertion.#ctor");

            // Edge 7: the negated method keeps the placeholder, as the class describes the positive
            // assertion; the positive method inherits.
            await Assert.That(allFiles).Contains("    /// <inheritdoc cref=\"T:TUnit.Assertions.Tests.TestData.NegatableDocumentedAssertion\"" + classPath);
            await Assert.That(allFiles).Contains("    /// Extension method for NegatableDocumentedAssertion." + Environment.NewLine
                + "    /// </summary>" + Environment.NewLine
                + "    public static NegatableDocumentedAssertion IsDocumentedFalse(");

            // Edge 8: the extension keeps the constructor's parameter names, so inherited <param> docs match.
            await Assert.That(allFiles).Contains("IsInDocumentedRange(this IAssertionSource<int> source, int minimum, int maximum,");

            await CompileChecker.AssertNoErrors(generatedFiles);
        });
}
