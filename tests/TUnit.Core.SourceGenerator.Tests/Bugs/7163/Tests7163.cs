namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// A generic test method whose type arguments its data source's declared type cannot determine must
/// be emitted as an entry whose rows fail with an explanation, instead of being dropped (#7163).
/// </summary>
internal class Tests7163 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
            Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7163",
            "UninferableGenericMethodTests.cs"),
        async generatedFiles =>
        {
            const string cannotInfer = "Could not infer the type arguments of generic test method 'Generic' from data source [MethodDataSource(nameof(Rows))]";

            string SourceFor(string className) => generatedFiles.Single(f =>
                f.Contains($"typeof(global::TUnit.TestProject.Bugs._7163.{className})"));

            // Uninferable rows: the data source is wrapped so each of its rows fails.
            foreach (var className in new[] { "ObjectRowsTests", "ObjectArrayRowsTests" })
            {
                var source = SourceFor(className);
                await Assert.That(source).Contains($"global::TUnit.Core.GenericConstraintViolationDataSource.Create(\"{cannotInfer}");
                await Assert.That(source).DoesNotContain("instance.Generic<");
            }

            // The inferable row still runs, beside the failing source.
            var partly = SourceFor("PartlyInferableTests");
            await Assert.That(partly).Contains("instance.Generic<int>(");
            await Assert.That(partly).Contains($"global::TUnit.Core.GenericConstraintViolationDataSource.Create(\"{cannotInfer}");

            // [GenerateGenericTest] lists the type arguments, so nothing is reported.
            var generateGeneric = SourceFor("GenerateGenericTestWithObjectRowsTests");
            await Assert.That(generateGeneric).Contains("instance.Generic<int>(");
            await Assert.That(generateGeneric).DoesNotContain("GenericConstraintViolationDataSource");

            // No data source: a single failing row.
            var noDataSource = SourceFor("NoDataSourceTests");
            await Assert.That(noDataSource).Contains("global::TUnit.Core.GenericConstraintViolationDataSource.Create(\"Could not infer the type arguments of generic test method 'Generic': it has no data source to infer them from.");

            // Data sources that infer the same type arguments as an earlier one join its instantiation.
            foreach (var className in new[] { "ArgumentsAndDataSourceWithSameTypesTests", "TwoDataSourcesWithSameTypesTests" })
            {
                var source = SourceFor(className);
                await Assert.That(source).Contains("instance.Generic<int>(");
                await Assert.That(source).DoesNotContain("GenericConstraintViolationDataSource");
            }

            // A return type that nests T is named by its open generic type, since T does not exist there.
            var nestedReturnType = SourceFor("NestedGenericReturnTypeTests");
            await Assert.That(nestedReturnType).Contains("typeof(global::System.Threading.Tasks.Task<>)");
            await Assert.That(nestedReturnType).DoesNotContain("Task<T[]>");
        });
}
