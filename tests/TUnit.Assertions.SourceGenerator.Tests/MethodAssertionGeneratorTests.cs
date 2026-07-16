using TUnit.Assertions.SourceGenerator.Generators;
using TUnit.Assertions.SourceGenerator.Tests.Options;

namespace TUnit.Assertions.SourceGenerator.Tests;

internal class MethodAssertionGeneratorTests : TestsBase<MethodAssertionGenerator>
{
    [Test]
    public Task BoolMethod() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "BoolMethodAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsPositive_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("Int_IsPositive_Assertion");
            await Assert.That(mainFile!).Contains("Int_IsGreaterThan_Int_Assertion");
            await Assert.That(mainFile!).Contains("public static Int_IsPositive_Assertion IsPositive");
        });

    [Test]
    public Task AssertionResultMethod() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AssertionResultMethodAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsEven_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("Int_IsEven_Assertion");
            await Assert.That(mainFile!).Contains("Int_IsBetween_Int_Int_Assertion");
            await Assert.That(mainFile!).Contains("return Task.FromResult(value!.IsEven())"); // AssertionResult wrapped in Task
        });

    [Test]
    public Task AsyncBoolMethod() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AsyncBoolAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsPositiveAsync_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("IsPositiveAsync_Assertion");
            await Assert.That(mainFile!).Contains("var result = await"); // Awaits Task<bool>
            // The Task<bool> branch's failure-return must terminate with a single `);` —
            // a `));` (two closing parens) is a literal C# syntax error that pins this
            // assertion against the regression where the emit had an extra closing paren.
            await Assert.That(mainFile!).DoesNotContain("Failed($\"found {value}\"));");
            await Assert.That(mainFile!).Contains("Failed($\"found {value}\");");
        });

    [Test]
    public Task AsyncAssertionResultMethod() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AsyncAssertionResultAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsEvenAsync_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("IsEvenAsync_Assertion");
            await Assert.That(mainFile!).Contains("return await"); // Awaits and returns
        });

    [Test]
    public Task GenericMethodWithNonInferableTypeParameter() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "GenericMethodWithNonInferableTypeParameter.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsErrorOfType"));
            await Assert.That(mainFile).IsNotNull();
            // Verify the method call includes type arguments
            await Assert.That(mainFile!).Contains("value!.IsErrorOfType<TValue, TError>()");
            // Verify the assertion class is generic
            await Assert.That(mainFile!).Contains("ResultTValue_IsErrorOfType_Assertion<TValue, TError>");
            // Verify the constraint is preserved
            await Assert.That(mainFile!).Contains("where TError : System.Exception");
        });

    [Test]
    public Task MethodWithComparableConstraint() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithComparableConstraint.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify IsGreaterThan generates with constraint
            await Assert.That(mainFile).Contains("Int_IsGreaterThan_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : System.IComparable<T>");
            await Assert.That(mainFile).Contains("IsGreaterThan<T>(this IAssertionSource<int> source");

            // Verify IsBetween generates with constraint
            await Assert.That(mainFile).Contains("Int_IsBetween_T_T_Assertion<T>");
            await Assert.That(mainFile).Contains("IsBetween<T>(this IAssertionSource<int> source");
        });

    [Test]
    public Task MethodWithMultipleConstraints() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithMultipleConstraints.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify all constraints are preserved
            await Assert.That(mainFile).Contains("String_HasProperty_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : class, System.IComparable<T>, new()");
            await Assert.That(mainFile).Contains("HasProperty<T>(this IAssertionSource<string> source");
        });

    [Test]
    public Task MethodWithReferenceTypeConstraint() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithReferenceTypeConstraint.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify class constraint is preserved
            await Assert.That(mainFile).Contains("String_IsNullOrDefault_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : class");
            await Assert.That(mainFile).Contains("IsNullOrDefault<T>(this IAssertionSource<string> source");
        });

    [Test]
    public Task MethodWithValueTypeConstraint() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithValueTypeConstraint.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify struct constraint is preserved
            await Assert.That(mainFile).Contains("Int_IsDefault_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : struct");
            await Assert.That(mainFile).Contains("IsDefault<T>(this IAssertionSource<int> source");
        });

    [Test]
    public Task MethodWithNotNullConstraint() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithNotNullConstraint.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify notnull constraint is preserved
            await Assert.That(mainFile).Contains("String_HasValue_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : notnull");
            await Assert.That(mainFile).Contains("HasValue<T>(this IAssertionSource<string> source");
        });

    [Test]
    public Task FileScopedClassWithInlining() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "FileScopedClassAssertion.cs"),
        async generatedFiles =>
        {
            // Snapshot test - the actual verification is done by snapshot comparison
            await Assert.That(generatedFiles.Count).IsGreaterThanOrEqualTo(1);
        });

    [Test]
    public Task MethodWithDefaultValues() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithDefaultValues.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify that the default value is preserved in the extension method
            await Assert.That(mainFile).Contains("bool exact = true");
        });

    [Test]
    public Task ValueTypeDefaultParameter() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "ValueTypeDefaultParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // A non-nullable value-type parameter declared with `= default` must render as
            // the bare `default` literal, not `= null`. The literal `null` is invalid for a
            // non-nullable value type and produces CS1750. The trailing comma anchors the
            // assertion to bare `default`, ruling out the longer `default(TypeName)` form.
            await Assert.That(mainFile).Contains("CancellationToken token = default,");
            await Assert.That(mainFile).DoesNotContain("CancellationToken token = null");

            await CompileChecker.AssertNoErrors(generatedFiles);
        });

#if NET8_0_OR_GREATER
    [Test]
    public Task RefStructParameter() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "RefStructParameterAssertion.cs"),
        new RunTestOptions { PreprocessorSymbols = ["NET8_0_OR_GREATER"] },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify that the field type is string, not the ref struct
            await Assert.That(mainFile).Contains("private readonly string _message;");
            await Assert.That(mainFile).Contains("private readonly string _suffix;");

            // Verify that the extension method converts the ref struct to string
            await Assert.That(mainFile).Contains("message.ToStringAndClear()");
            await Assert.That(mainFile).Contains("suffix.ToStringAndClear()");

            // Verify the constructor takes string, not the ref struct
            await Assert.That(mainFile).Contains("string message)");
            await Assert.That(mainFile).Contains("string suffix)");

            // Verify that .ToStringAndClear() is removed in the inlined body
            // (since the field is already a string)
            // The inlined body should use _message directly, not _message.ToStringAndClear()
            await Assert.That(mainFile).Contains("value!.Contains(_message)");
        });
#endif

    [Test]
    public Task AssertionResultOfTMethod() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AssertionResultOfTMethodAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).Contains("IEnumerableString_ContainsMatch_String_Assertion");
            // Terminal assertion: should have _result field
            await Assert.That(mainFile).Contains("private string? _result;");
            // Terminal assertion: should have new GetAwaiter
            await Assert.That(mainFile).Contains("public new System.Runtime.CompilerServices.TaskAwaiter<string> GetAwaiter()");
            // Should unwrap AssertionResult<T> to AssertionResult
            await Assert.That(mainFile).Contains("var typedResult =");
            await Assert.That(mainFile).Contains("_result = typedResult.Value;");
        });

    [Test]
    public Task AsyncAssertionResultOfTMethod() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AsyncAssertionResultOfTAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).Contains("IEnumerableString_ContainsMatchAsync_String_Assertion");
            // Terminal assertion: should have _result field
            await Assert.That(mainFile).Contains("private string? _result;");
            // Terminal assertion: should have new GetAwaiter
            await Assert.That(mainFile).Contains("public new System.Runtime.CompilerServices.TaskAwaiter<string> GetAwaiter()");
            // Async: should await the method call
            await Assert.That(mainFile).Contains("var typedResult = await");
            await Assert.That(mainFile).Contains("_result = typedResult.Value;");
        });

    [Test]
    public Task ArrayTargetType() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "ArrayTargetAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("ContainsMessage"));
            await Assert.That(mainFile).IsNotNull();
            // Verify extension method targets IAssertionSource<string[]>
            await Assert.That(mainFile!).Contains("IAssertionSource<string[]>");
            await Assert.That(mainFile!).Contains("StringArray_ContainsMessage_String_Bool_Assertion");
        });

    [Test]
    public Task ParamsParameter() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "ParamsParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Edge 1: canonical CS0231 shape: required parameter followed by params.
            // The [CallerArgumentExpression(nameof(label))] block must sit BEFORE the
            // params array; if the order ever regresses, this single substring fails
            // ahead of the compile-clean gate below.
            await Assert.That(mainFile).Contains(
                "ContainsAny(this IAssertionSource<string> source, string label, [CallerArgumentExpression(nameof(label))] string? labelExpression = null, params string[] candidates)");

            // Edge 2: multiple required parameters preceding params. Every diagnostic
            // parameter is emitted, in source order, ahead of the params array.
            await Assert.That(mainFile).Contains(
                "IsBetweenExcluding(this IAssertionSource<int> source, int min, int max, [CallerArgumentExpression(nameof(min))] string? minExpression = null, [CallerArgumentExpression(nameof(max))] string? maxExpression = null, params int[] excluded)");

            // Edge 3: optional defaulted parameter before params. The default value
            // is preserved and the diagnostic parameter still precedes params.
            await Assert.That(mainFile).Contains(
                "MeetsLength(this IAssertionSource<string> source, int minLength = 1, [CallerArgumentExpression(nameof(minLength))] string? minLengthExpression = null, params string[] suffixes)");

            // Edge 4: generic-typed parameters. The generic substitutions are
            // preserved and the diagnostic for the required generic param precedes
            // the generic params array.
            await Assert.That(mainFile).Contains(
                "IsOneOfWithDefault<T>(this IAssertionSource<T> source, T fallback, [CallerArgumentExpression(nameof(fallback))] string? fallbackExpression = null, params T[] alternatives)");

            // Edge 5: InlineMethodBody path. The same emit ordering applies when
            // the body is inlined (not delegated through a stored helper).
            await Assert.That(mainFile).Contains(
                "StartsWithAny(this IAssertionSource<string> source, string prefix, [CallerArgumentExpression(nameof(prefix))] string? prefixExpression = null, params string[] suffixes)");

            // Edge 6: params-only. Byte-identical to the pre-fix shape used by
            // IsIn/IsNotIn in production. No diagnostic parameter is emitted at
            // all because the params parameter itself cannot be auto-supplied,
            // and there is no preceding non-params parameter that could.
            await Assert.That(mainFile).Contains(
                "ContainsExactly(this IAssertionSource<string> source, params string[] required)");
            await Assert.That(mainFile).DoesNotContain("requiredExpression");

            // Structural gate: parse the emitted code as C# and fail on any error
            // diagnostic. This is the regression sentinel for CS0231 plus any other
            // emit defect (mis-paired brackets, invalid generic argument lists, etc.)
            // that a content-only assertion cannot see.
            await CompileChecker.AssertNoErrors(generatedFiles);
        });

    [Test]
    public Task MethodOnConcreteNonSealedReceiver() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodOnConcreteNonSealedReceiver.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).Count().IsEqualTo(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // The generated extension method must declare a single type parameter (T from the
            // source method) and target the exact receiver type. Prepending the covariant
            // receiver-type parameter (TActual) for this shape produces a two-type-parameter
            // signature that callers cannot satisfy via partial type-argument specification,
            // breaking call sites like .HasItem<int>(42) with CS1929.
            await Assert.That(mainFile).Contains("HasItem<T>(this IAssertionSource<TUnit.Assertions.Tests.TestData.MethodOnConcreteNonSealedReceiver> source");
            await Assert.That(mainFile).DoesNotContain("HasItem<TActual, T>");
            await Assert.That(mainFile).DoesNotContain("where TActual :");
        });
}
