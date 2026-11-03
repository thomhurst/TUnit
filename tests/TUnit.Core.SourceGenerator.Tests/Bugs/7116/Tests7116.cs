namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Type arguments inferred from a data source's broad declared type that break a constraint are
/// reported with an explanation of declared-type inference, and a method-level [GenerateGenericTest]
/// runs those rows on its own type arguments instead of reporting them (#7116).
/// </summary>
internal class Tests7116 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
            Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7116",
            "DeclaredTypeInferenceTests.cs"),
        async generatedFiles =>
        {
            const string violation = "global::TUnit.Core.GenericConstraintViolationDataSource.Create(";

            string SourceFor(string methodName) => generatedFiles.Single(f =>
                f.Contains($"methodName: \"{methodName}<") || f.Contains($"methodName: \"{methodName}\""));

            await Assert.That(SourceFor("ObjectDeclaredType"))
                .Contains(violation + "\"Type 'Object' does not implement interface 'IDisposable' required for generic parameter 'T'. 'T' was inferred from the declared type 'object' of the data source");

            await Assert.That(SourceFor("NullableDeclaredType"))
                .Contains("'T' was inferred from the declared type 'int?' of the data source");

            // A typed data source (DataSourceGeneratorAttribute<object>) gets the same explanation.
            await Assert.That(SourceFor("TypedObjectDeclaredType"))
                .Contains("'T' was inferred from the declared type 'object' of the data source");

            foreach (var methodName in new[] { "GenerateGenericTestWithObjectRows", "GenerateGenericTestWithSingleObjectRows" })
            {
                var source = SourceFor(methodName);
                await Assert.That(source).DoesNotContain(violation);
                await Assert.That(source).Contains($"instance.{methodName}<global::System.IO.MemoryStream>(");
            }

            // Both sources' rows run on the [GenerateGenericTest] type, including the object-declared one.
            var exactAndObject = SourceFor("GenerateGenericTestWithExactAndObjectRows");
            await Assert.That(exactAndObject).DoesNotContain(violation);
            await Assert.That(exactAndObject).Contains("DeclaredTypeInferenceTests.ExactStreams()");
            await Assert.That(exactAndObject).Contains("DeclaredTypeInferenceTests.Streams()");

            // An empty or wrongly sized [GenerateGenericTest] fails on its own, so the rows are not silently lost.
            // As in reflection mode, the object-declared rows are not reported as well (#7222).
            await Assert.That(SourceFor("EmptyGenerateGenericTestWithObjectRows"))
                .Contains(violation + "\"[GenerateGenericTest] on method 'EmptyGenerateGenericTestWithObjectRows' has no type arguments\")");
            await Assert.That(SourceFor("WrongArityGenerateGenericTestWithObjectRows"))
                .Contains(violation + "\"[GenerateGenericTest] on method 'WrongArityGenerateGenericTestWithObjectRows' provides 2 type argument(s) but method requires 1. Provided: [MemoryStream, Int32]\")");
            foreach (var methodName in new[] { "EmptyGenerateGenericTestWithObjectRows", "WrongArityGenerateGenericTestWithObjectRows" })
            {
                await Assert.That(SourceFor(methodName))
                    .DoesNotContain("'T' was inferred from the declared type 'object' of the data source");
            }
        });
}
