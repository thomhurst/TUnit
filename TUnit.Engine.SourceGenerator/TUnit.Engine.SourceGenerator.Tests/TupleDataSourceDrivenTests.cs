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
            
            Assert.That(generatedFiles[0], Does.Contain("var methodArg0 = global::TUnit.TestProject.TupleDataSourceDrivenTests.TupleMethod();"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg1 = methodArg0.Item1;"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.String methodArg2 = methodArg0.Item2;"));
            Assert.That(generatedFiles[0], Does.Contain("global::System.Boolean methodArg3 = methodArg0.Item3;"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodArg1,methodArg2,methodArg3],"));
            Assert.That(generatedFiles[0], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg1}, {methodArg2}, {methodArg3})\","));
            Assert.That(generatedFiles[0], Does.Contain("TestBody = classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.DataSource_TupleMethod(methodArg1,methodArg2,methodArg3)),"));
        });
}