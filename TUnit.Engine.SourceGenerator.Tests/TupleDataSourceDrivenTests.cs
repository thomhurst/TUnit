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
            
            Assert.That(generatedFiles[0], Does.Contain("(global::System.Int32, global::System.String, global::System.Boolean) methodArg0 = global::TUnit.TestProject.TupleDataSourceDrivenTests.TupleMethod();"));
            Assert.That(generatedFiles[0], Does.Contain("var (methodArg1, methodArg2, methodArg3) = methodArg0;"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodArg1, methodArg2, methodArg3],"));
            Assert.That(generatedFiles[0], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg1}, {methodArg2}, {methodArg3})\","));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.DataSource_TupleMethod(methodArg1, methodArg2, methodArg3))"));
        });
}