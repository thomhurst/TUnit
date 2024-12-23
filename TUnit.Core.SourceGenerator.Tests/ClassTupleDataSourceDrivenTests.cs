using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ClassTupleDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [TestCase(0, "TupleMethod", "TupleMethod")]
    [TestCase(0, "NamedTupleMethod", "TupleMethod")]
    [TestCase(0, "TupleMethod", "NamedTupleMethod")]
    [TestCase(0, "NamedTupleMethod", "NamedTupleMethod")]
    public Task Test(int index, string classMethodName, string testMethodName) => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassTupleDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            
            await Verify(generatedFiles[index]);
        });
}