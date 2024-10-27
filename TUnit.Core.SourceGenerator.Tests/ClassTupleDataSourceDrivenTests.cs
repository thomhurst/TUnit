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
            
            await AssertFileContains(generatedFiles[index], $"var classArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.{classMethodName}());");
            await AssertFileContains(generatedFiles[index], "global::System.Int32 classArg = classArgTuples.Item1;");
            await AssertFileContains(generatedFiles[index], "global::System.String classArg1 = classArgTuples.Item2;");
            await AssertFileContains(generatedFiles[index], "global::System.Boolean classArg2 = classArgTuples.Item3;");
            await AssertFileContains(generatedFiles[index], "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassTupleDataSourceDrivenTests>(() => new global::TUnit.TestProject.ClassTupleDataSourceDrivenTests(classArg, classArg1, classArg2)\t\t\t{\n\t\t\t\tProperty1 = propertyArg,\n\t\t\t\tProperty2 = propertyArg1,\n\t\t\t\tProperty3 = propertyArg2,\n\t\t\t\tProperty4 = propertyArg3,\n\t\t\t}\n, sessionId);");
            
            await AssertFileContains(generatedFiles[index], $"var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.{testMethodName}());");
            await AssertFileContains(generatedFiles[index], "global::System.Int32 methodArg = methodArgTuples.Item1;");
            await AssertFileContains(generatedFiles[index], "global::System.String methodArg1 = methodArgTuples.Item2;");
            await AssertFileContains(generatedFiles[index], "global::System.Boolean methodArg2 = methodArgTuples.Item3;");
            
            await AssertFileContains(generatedFiles[index], "TestMethodArguments = [methodArg, methodArg1, methodArg2],");
            await AssertFileContains(generatedFiles[index],
                "TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg, methodArg1, methodArg2))");
                
            await AssertFileContains(generatedFiles[index], "(global::System.Int32, global::System.String, global::System.Boolean) propertyArg = global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.TupleMethod();");
            await AssertFileContains(generatedFiles[index], "(global::System.Int32 Number, global::System.String Word, global::System.Boolean Flag) propertyArg1 = global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.NamedTupleMethod();");

        });
}