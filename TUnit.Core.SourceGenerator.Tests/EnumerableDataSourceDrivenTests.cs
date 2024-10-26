using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class EnumerableDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "EnumerableDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);
            
            await AssertFileContains(generatedFiles[0], "foreach (var methodData in global::TUnit.TestProject.EnumerableDataSourceDrivenTests.SomeMethod())");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodData],");
            
            await AssertFileContains(generatedFiles[1], "foreach (var methodData in global::TUnit.TestProject.EnumerableDataSourceDrivenTests.SomeMethod())");
            await AssertFileContains(generatedFiles[1], "TestMethodArguments = [methodData],");
        });
}