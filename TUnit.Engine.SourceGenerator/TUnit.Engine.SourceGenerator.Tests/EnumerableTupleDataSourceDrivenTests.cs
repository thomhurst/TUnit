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
            Assert.That(generatedFiles.Length, Is.EqualTo(1));
            
            Assert.That(generatedFiles[0], Does.Contain("foreach (var methodData in global::TUnit.TestProject.EnumerableTupleDataSourceDrivenTests.TupleMethod())"));
            Assert.That(generatedFiles[0], Does.Contain("var methodArg0 = methodData;"));
            Assert.That(generatedFiles[0], Does.Contain("var (methodArg1, methodArg2, methodArg3) = methodArg0;"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodArg1, methodArg2, methodArg3],"));
            Assert.That(generatedFiles[0], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg1}, {methodArg2}, {methodArg3})\","));
            Assert.That(generatedFiles[0], Does.Contain("TestBody = classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.DataSource_TupleMethod(methodArg1, methodArg2, methodArg3)),"));
        });
}