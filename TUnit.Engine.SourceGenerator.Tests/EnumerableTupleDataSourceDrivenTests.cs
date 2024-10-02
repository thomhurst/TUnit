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
            
            Assert.That(generatedFiles[0], Does.Contain("foreach (var methodData in global::TUnit.TestProject.EnumerableTupleDataSourceDrivenTests.TupleMethod())"));
            Assert.That(generatedFiles[0], Does.Contain("var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(methodData);"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg0 = methodArgTuples.Item1;"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.String methodArg1 = methodArgTuples.Item2;"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Boolean methodArg2 = methodArgTuples.Item3;"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodArg0, methodArg1, methodArg2],"));
            Assert.That(generatedFiles[0], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg0}, {methodArg1}, {methodArg2})\","));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg0, methodArg1, methodArg2))"));
            
            Assert.That(generatedFiles[1], Does.Contain("foreach (var methodData in global::TUnit.TestProject.EnumerableTupleDataSourceDrivenTests.NamedTupleMethod())"));
            Assert.That(generatedFiles[0], Does.Contain("var methodArgTuples = global::System.TupleExtensions.ToTuple<global::System.Int32, global::System.String, global::System.Boolean>(methodData);"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg0 = methodArgTuples.Item1;"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.String methodArg1 = methodArgTuples.Item2;"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Boolean methodArg2 = methodArgTuples.Item3;"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodArg0, methodArg1, methodArg2],"));
            Assert.That(generatedFiles[1], Does.Contain("TestMethodArguments = [methodArg0, methodArg1, methodArg2],"));
            Assert.That(generatedFiles[1], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg0}, {methodArg1}, {methodArg2})\","));
            Assert.That(generatedFiles[1], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg0, methodArg1, methodArg2))"));
        });
}