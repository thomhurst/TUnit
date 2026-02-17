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
            await Assert.That(generatedFiles).HasCount(1);

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
            await Assert.That(generatedFiles).HasCount(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsEven_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("Int_IsEven_Assertion");
            await Assert.That(mainFile!).Contains("Int_IsBetween_Int_Int_Assertion");
            await Assert.That(mainFile!).Contains("return Task.FromResult(value!.IsEven())"); // AssertionResult wrapped in Task
        });

    [Test]
    public Task AsyncBoolMethod() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "AsyncBoolAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

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
            await Assert.That(generatedFiles).HasCount(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("IsEvenAsync_Assertion"));
            await Assert.That(mainFile).IsNotNull();
            await Assert.That(mainFile!).Contains("IsEvenAsync_Assertion");
            await Assert.That(mainFile!).Contains("return await"); // Awaits and returns
        });

    [Test]
    public Task GenericMethodWithNonInferableTypeParameter() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "GenericMethodWithNonInferableTypeParameter.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

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
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithComparableConstraint.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

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
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithMultipleConstraints.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify all constraints are preserved
            await Assert.That(mainFile).Contains("String_HasProperty_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : class, System.IComparable<T>, new()");
            await Assert.That(mainFile).Contains("HasProperty<T>(this IAssertionSource<string> source");
        });

    [Test]
    public Task MethodWithReferenceTypeConstraint() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithReferenceTypeConstraint.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify class constraint is preserved
            await Assert.That(mainFile).Contains("String_IsNullOrDefault_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : class");
            await Assert.That(mainFile).Contains("IsNullOrDefault<T>(this IAssertionSource<string> source");
        });

    [Test]
    public Task MethodWithValueTypeConstraint() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithValueTypeConstraint.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify struct constraint is preserved
            await Assert.That(mainFile).Contains("Int_IsDefault_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : struct");
            await Assert.That(mainFile).Contains("IsDefault<T>(this IAssertionSource<int> source");
        });

    [Test]
    public Task MethodWithNotNullConstraint() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithNotNullConstraint.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify notnull constraint is preserved
            await Assert.That(mainFile).Contains("String_HasValue_T_Assertion<T>");
            await Assert.That(mainFile).Contains("where T : notnull");
            await Assert.That(mainFile).Contains("HasValue<T>(this IAssertionSource<string> source");
        });

    [Test]
    public Task FileScopedClassWithInlining() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
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
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "MethodWithDefaultValues.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

            var mainFile = generatedFiles.First();
            await Assert.That(mainFile).IsNotNull();

            // Verify that the default value is preserved in the extension method
            await Assert.That(mainFile).Contains("bool exact = true");
        });

#if NET6_0_OR_GREATER
    [Test]
    public Task RefStructParameter() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "RefStructParameterAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

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
    public Task ArrayTargetType() => RunTest(
        Path.Combine(Sourcy.Git.RootDirectory.FullName,
            "TUnit.Assertions.SourceGenerator.Tests",
            "TestData",
            "ArrayTargetAssertion.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount(1);

            var mainFile = generatedFiles.FirstOrDefault(f => f.Contains("ContainsMessage"));
            await Assert.That(mainFile).IsNotNull();
            // Verify extension method targets IAssertionSource<string[]>
            await Assert.That(mainFile!).Contains("IAssertionSource<string[]>");
            await Assert.That(mainFile!).Contains("StringArray_ContainsMessage_String_Bool_Assertion");
        });
}
