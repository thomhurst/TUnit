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
            // Violation entries are named after the method alone (as in reflection mode); valid ones
            // carry the type arguments.
            string SourceFor(string methodName) => generatedFiles.Single(f =>
                f.Contains($"methodName: \"{methodName}<") || f.Contains($"methodName: \"{methodName}\""));

            const string violation = "global::TUnit.Core.GenericConstraintViolationDataSource.Create(";
            static int Count(string source, string value) => (source.Length - source.Replace(value, "").Length) / value.Length;

            // Each invalid entry wraps the data source that inferred the invalid type, so that source's rows still drive the test count.
            await Assert.That(SourceFor("GenericMethodWithClassConstraint_InvalidType"))
                .Contains(violation + "\"Type 'Int32' does not satisfy the 'class' constraint for generic parameter 'T'\", new global::TUnit.TestProject.IntDataSourceForConstraint())");

            await Assert.That(SourceFor("GenericMethodWithInterfaceConstraint_InvalidType"))
                .Contains(violation + "\"Type 'NonImplementingClass' does not implement interface 'ITestInterface' required for generic parameter 'T'\", new global::TUnit.TestProject.NonImplementingClassDataSource())");

            await Assert.That(SourceFor("GenericMethodWithStructConstraint_InvalidType"))
                .Contains(violation + "\"Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'\", new global::TUnit.Core.ArgumentsAttribute(\"invalid\"))");

            // The valid row keeps its real data source and call; only the invalid row is replaced.
            var mixedRows = SourceFor("GenericMethodWithStructConstraint_MixedRows");
            await Assert.That(Count(mixedRows, violation)).IsEqualTo(1);
            await Assert.That(mixedRows).Contains("instance.GenericMethodWithStructConstraint_MixedRows<int>(");
            await Assert.That(mixedRows).DoesNotContain("instance.GenericMethodWithStructConstraint_MixedRows<string>(");

            await Assert.That(SourceFor("GenericMethodWithStructConstraint_ValidType")).DoesNotContain(violation);

            // Two attributes that infer the same invalid type: both are kept, one wrapper each.
            await Assert.That(Count(SourceFor("TwoInvalidArgumentsRows"), violation)).IsEqualTo(2);

            // A source that needs the test class instance is wrapped too; Create keeps that need visible to the engine.
            await Assert.That(SourceFor("TwoInvalidInstanceDataSourceRows"))
                .Contains(violation + "\"Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'\", new global::TUnit.TestProject.InstanceTwoStringsDataSourceForConstraint())");

            // Legal type arguments that match a constraint exactly or through variance are not reported.
            await Assert.That(SourceFor("ExactBaseTypeConstraintMatch")).DoesNotContain(violation);
            await Assert.That(SourceFor("ExactInterfaceConstraintMatch")).DoesNotContain(violation);
            await Assert.That(SourceFor("VariantInterfaceConstraintMatch")).DoesNotContain(violation);
            await Assert.That(SourceFor("ArrayCovariantInterfaceConstraintMatch")).DoesNotContain(violation);
            await Assert.That(SourceFor("NestedVariantInterfaceConstraintMatch")).DoesNotContain(violation);
            await Assert.That(SourceFor("DelegateVariantInterfaceConstraintMatch")).DoesNotContain(violation);
            await Assert.That(SourceFor("ArrayOfTypeParameterConstraintMatch")).DoesNotContain(violation);

            // A nested interface's containing type arguments must match too: this row is reported, not emitted as a call.
            var nestedMismatch = SourceFor("NestedInterfaceWithOtherContainingTypeArgument");
            await Assert.That(nestedMismatch).Contains(violation + "\"Type 'MismatchedOuterPayload' does not implement interface 'IOut`1' required for generic parameter 'T'\"");
            await Assert.That(nestedMismatch).DoesNotContain("instance.NestedInterfaceWithOtherContainingTypeArgument<");

            // Generic class + generic method [Arguments]: the invalid method row gets a failing entry.
            var classAndMethodRows = SourceFor("MixedMethodRows");
            await Assert.That(Count(classAndMethodRows, violation)).IsEqualTo(1);
            await Assert.That(classAndMethodRows).Contains(violation + "\"Type 'String' does not satisfy the 'struct' constraint for generic parameter 'TMethod'\"");

            // A method constraint that mentions the class type parameter is checked with the class type argument:
            // TMethod=int satisfies IComparable<int> and is called, TMethod=string is reported.
            var classTypeParameterRows = SourceFor("ClassTypeParameterConstraintRows");
            await Assert.That(Count(classTypeParameterRows, violation)).IsEqualTo(1);
            await Assert.That(classTypeParameterRows).Contains("instance.ClassTypeParameterConstraintRows<int>(");
            await Assert.That(classTypeParameterRows).Contains(violation + "\"Type 'String' does not implement interface 'IComparable`1' required for generic parameter 'TMethod'\"");
        });
}
