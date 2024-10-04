using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class EnumerableTupleDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "EnumerableTupleDataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles, Has.Length.EqualTo(2));
            
            AssertFileContains(generatedFiles[0], "foreach (var methodData in global::TUnit.TestProject.EnumerableTupleDataSourceDrivenTests.TupleMethod())");
            AssertFileContains(generatedFiles[0], "var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(methodData);");
            AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = methodArgTuples.Item1;");
            AssertFileContains(generatedFiles[0], "global::System.String methodArg1 = methodArgTuples.Item2;");
            AssertFileContains(generatedFiles[0], "global::System.Boolean methodArg2 = methodArgTuples.Item3;");
            AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg, methodArg1, methodArg2],");
            AssertFileContains(generatedFiles[0], "DisplayName = $\"DataSource_TupleMethod({methodArg}, {methodArg1}, {methodArg2})\",");
            AssertFileContains(generatedFiles[0], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg, methodArg1, methodArg2))");
            
            AssertFileContains(generatedFiles[1], "foreach (var methodData in global::TUnit.TestProject.EnumerableTupleDataSourceDrivenTests.NamedTupleMethod())");
            AssertFileContains(generatedFiles[0], "var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(methodData);");
            AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = methodArgTuples.Item1;");
            AssertFileContains(generatedFiles[0], "global::System.String methodArg1 = methodArgTuples.Item2;");
            AssertFileContains(generatedFiles[0], "global::System.Boolean methodArg2 = methodArgTuples.Item3;");
            AssertFileContains(generatedFiles[0], "TestMethodArguments = [methodArg, methodArg1, methodArg2],");
            AssertFileContains(generatedFiles[1], "TestMethodArguments = [methodArg, methodArg1, methodArg2],");
            AssertFileContains(generatedFiles[1], "DisplayName = $\"DataSource_TupleMethod({methodArg}, {methodArg1}, {methodArg2})\",");
            AssertFileContains(generatedFiles[1], "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg, methodArg1, methodArg2))");
        });
}