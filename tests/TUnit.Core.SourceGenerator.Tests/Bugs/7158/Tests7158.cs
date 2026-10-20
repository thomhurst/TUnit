namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// A generic method with several [GenerateGenericTest] types and one typed ClassDataSource&lt;T&gt;
/// per type pairs each instantiation only with the data source of its type (#7158).
/// </summary>
internal class Tests7158 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
            Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7158",
            "GenerateGenericTestTypedClassDataSourceTests.cs"),
        async generatedFiles =>
        {
            var source = generatedFiles.Single(f => f.Contains("GenerateGenericTestTypedClassDataSourceTests"));
            const string dataSourceA = "new global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.Bugs._7158.Gen7158A>()";
            const string dataSourceB = "new global::TUnit.Core.ClassDataSourceAttribute<global::TUnit.TestProject.Bugs._7158.Gen7158B>()";

            // Each entry's test data sources hold only the data source of its own type argument.
            var entryA = source.Substring(source.IndexOf("methodName: \"Run<Gen7158A>\"", StringComparison.Ordinal));
            entryA = entryA.Substring(0, entryA.IndexOf("methodMetadata:", StringComparison.Ordinal));
            var entryB = source.Substring(source.IndexOf("methodName: \"Run<Gen7158B>\"", StringComparison.Ordinal));
            entryB = entryB.Substring(0, entryB.IndexOf("methodMetadata:", StringComparison.Ordinal));

            await Assert.That(entryA).Contains(dataSourceA);
            await Assert.That(entryA).DoesNotContain(dataSourceB);
            await Assert.That(entryB).Contains(dataSourceB);
            await Assert.That(entryB).DoesNotContain(dataSourceA);
        });
}
