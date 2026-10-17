namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Class type arguments that break the class's constraints, and method type arguments from
/// [GenerateGenericTest] or IInfersType&lt;T&gt; that break the method's constraints, must be emitted as
/// failing entries instead of being dropped (#7107).
/// </summary>
internal class Tests7107 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
            Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7107",
            "GenericClassConstraintViolationTests.cs"),
        async generatedFiles =>
        {
            const string structViolation = "Type 'String' does not satisfy the 'struct' constraint for generic parameter 'T'";

            // Generated class names are truncated and hashed, so locate each file by its class name.
            string SourceFor(string className) => generatedFiles.Single(f =>
                f.Contains($"global::TUnit.TestProject.Bugs._7107.{className}"));

            // Class rows and class-level [GenerateGenericTest]: the invalid type is registered against
            // the open generic type, while the valid type keeps its TestEntry.
            foreach (var className in new[] { "ClassRowConstraintTests", "ClassGenerateGenericConstraintTests", "MethodRowInfersClassConstraintTests" })
            {
                var source = SourceFor(className);
                await Assert.That(source).Contains($"RegisterGenericConstraintViolations(typeof(global::TUnit.TestProject.Bugs._7107.{className}<>)");
                await Assert.That(source).Contains($"Messages = new string[] {{ \"{structViolation}\" }}");
                await Assert.That(source).Contains($"RegisterEntries<global::TUnit.TestProject.Bugs._7107.{className}<int>>");
                await Assert.That(source).DoesNotContain($"{className}<string>");
            }

            // The only class row is invalid: no TestEntry, only the violation.
            var onlyInvalid = SourceFor("OnlyInvalidClassRowConstraintTests");
            await Assert.That(onlyInvalid).Contains("RegisterGenericConstraintViolations(");
            await Assert.That(onlyInvalid).DoesNotContain("RegisterEntries<");

            // Each invalid class row is reported once per test method, in declaration order.
            await Assert.That(generatedFiles.Where(f => f.Contains("SeveralInvalidClassRowsConstraintTests<>")).ToArray())
                .All(f => f.Contains(
                    $"Messages = new string[] {{ \"{structViolation}\", \"Type 'DayOfWeek' does not implement interface 'IEquatable`1' required for generic parameter 'T'\", \"{structViolation}\" }}"));

            // Method-level [GenerateGenericTest]: one single-row violation data source.
            var methodGenerateGeneric = SourceFor("MethodGenerateGenericConstraintTests");
            await Assert.That(methodGenerateGeneric).Contains($"new global::TUnit.Core.GenericConstraintViolationDataSource(\"{structViolation}\"),");
            await Assert.That(methodGenerateGeneric).Contains("instance.Run<int>(");

            // IInfersType<T>: the violation wraps the matrix source, so each matrix row fails.
            var matrix = generatedFiles.Single(f => f.Contains("methodName: \"Invalid\""));
            await Assert.That(matrix).Contains($"new global::TUnit.Core.GenericConstraintViolationDataSource(\"{structViolation}\", new global::TUnit.Core.MatrixDataSourceAttribute()");

            // Two invalid method rows inferring the same type: both rows are wrapped in one entry.
            var severalMethodRows = SourceFor("SeveralInvalidMethodRowsConstraintTests");
            await Assert.That(severalMethodRows.Split(["GenericConstraintViolationDataSource("], StringSplitOptions.None).Length - 1).IsEqualTo(2);
        });
}
