namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Data rows whose inferred method type arguments break the method's generic constraints must be
/// emitted as entries that fail with the constraint message, not dropped (#7097).
/// </summary>
internal class GenericConstraintValidationTests : TestsBase
{
    [Test]
    public Task Constraint_Violating_Rows_Emit_Failing_Entries() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "GenericConstraintValidationTests.cs"),
        async generatedFiles =>
        {
            // Generated class names are truncated and hashed, so locate each file by its entry's method name.
            string SourceFor(string methodName) => generatedFiles.Single(f =>
                f.Contains($"methodName: \"{methodName}<"));

            await Assert.That(SourceFor("GenericMethodWithClassConstraint_InvalidType"))
                .Contains("new global::TUnit.Core.GenericConstraintViolationDataSource(\"Type 'Int32' does not satisfy the 'class' constraint for generic parameter 'T'\")");

            await Assert.That(SourceFor("GenericMethodWithInterfaceConstraint_InvalidType"))
                .Contains("new global::TUnit.Core.GenericConstraintViolationDataSource(\"Type 'NonImplementingClass' does not implement interface 'ITestInterface' required for generic parameter 'T'\")");

            await Assert.That(SourceFor("GenericMethodWithStructConstraint_InvalidType"))
                .Contains("new global::TUnit.Core.GenericConstraintViolationDataSource(\"Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'\")");

            // The valid row keeps its real data source and call; only the invalid row is replaced.
            var mixedRows = SourceFor("GenericMethodWithStructConstraint_MixedRows");
            await Assert.That(mixedRows).Contains("GenericConstraintViolationDataSource");
            await Assert.That(mixedRows).Contains("instance.GenericMethodWithStructConstraint_MixedRows<int>(");
            await Assert.That(mixedRows).DoesNotContain("instance.GenericMethodWithStructConstraint_MixedRows<string>(");

            await Assert.That(SourceFor("GenericMethodWithStructConstraint_ValidType")).DoesNotContain("GenericConstraintViolationDataSource");
        });
}
