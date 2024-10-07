using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class TupleDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TupleDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            
            await AssertFileContains(generatedFiles[0], "var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(global::TUnit.TestProject.TupleDataSourceDrivenTests.TupleMethod());");
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = methodArgTuples.Item1;");
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg1 = methodArgTuples.Item2;");
            await AssertFileContains(generatedFiles[0], "global::System.Boolean methodArg2 = methodArgTuples.Item3;");
            await AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg, methodArg1, methodArg2],");
            await AssertFileContains(generatedFiles[0], "DisplayName = $\"DataSource_TupleMethod({methodArg}, {methodArg1}, {methodArg2})\",");
            await AssertFileContains(generatedFiles[0], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg, methodArg1, methodArg2))");
        });
}