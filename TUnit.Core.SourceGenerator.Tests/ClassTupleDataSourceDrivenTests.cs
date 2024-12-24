using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassTupleDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    [Arguments(0, "TupleMethod", "TupleMethod")]
    [Arguments(0, "NamedTupleMethod", "TupleMethod")]
    [Arguments(0, "TupleMethod", "NamedTupleMethod")]
    [Arguments(0, "NamedTupleMethod", "NamedTupleMethod")]
    public Task Test(int index, string classMethodName, string testMethodName) => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassTupleDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);

        });
}