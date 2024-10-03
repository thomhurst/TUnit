using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassTupleDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [TestCase(0, "TupleMethod", "TupleMethod")]
    [TestCase(1, "NamedTupleMethod", "TupleMethod")]
    [TestCase(2, "TupleMethod", "NamedTupleMethod")]
    [TestCase(3, "NamedTupleMethod", "NamedTupleMethod")]
    public Task Test(int index, string classMethodName, string testMethodName) => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassTupleDataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(4));
            
            Assert.That(generatedFiles[index], Does.Contain($"var classArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.{classMethodName}());"));
            Assert.That(generatedFiles[index], Does.Contain("global::System.Int32 classArg = classArgTuples.Item1;"));
            Assert.That(generatedFiles[index], Does.Contain("global::System.String classArg1 = classArgTuples.Item2;"));
            Assert.That(generatedFiles[index], Does.Contain("global::System.Boolean classArg2 = classArgTuples.Item3;"));
            Assert.That(generatedFiles[index], Does.Contain("var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassTupleDataSourceDrivenTests>(() => new global::TUnit.TestProject.ClassTupleDataSourceDrivenTests(classArg, classArg1, classArg2));"));
            
            Assert.That(generatedFiles[index], Does.Contain($"var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.{testMethodName}());"));
            Assert.That(generatedFiles[index], Does.Contain("global::System.Int32 methodArg = methodArgTuples.Item1;"));
            Assert.That(generatedFiles[index], Does.Contain("global::System.String methodArg1 = methodArgTuples.Item2;"));
            Assert.That(generatedFiles[index], Does.Contain("global::System.Boolean methodArg2 = methodArgTuples.Item3;"));
            
            Assert.That(generatedFiles[index], Does.Contain("TestMethodArguments = [methodArg, methodArg1, methodArg2],"));
            Assert.That(generatedFiles[index], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg}, {methodArg1}, {methodArg2})\","));
            Assert.That(generatedFiles[index], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg, methodArg1, methodArg2))"));
        });
}