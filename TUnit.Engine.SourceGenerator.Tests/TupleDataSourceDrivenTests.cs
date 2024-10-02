using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class TupleDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TupleDataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(1));
            
            Assert.That(generatedFiles[0], Does.Contain("var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(global::TUnit.TestProject.TupleDataSourceDrivenTests.TupleMethod());"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg0 = methodArgTuples.Item1;"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.String methodArg1 = methodArgTuples.Item2;"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Boolean methodArg2 = methodArgTuples.Item3;"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodArg0, methodArg1, methodArg2],"));
            Assert.That(generatedFiles[0], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg0}, {methodArg1}, {methodArg2})\","));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg0, methodArg1, methodArg2))"));
        });
}