using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassTupleDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassTupleDataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(1));

            Assert.That(generatedFiles[0], Does.Contain("var classArg0 = global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.TupleMethod();"));
            Assert.That(generatedFiles[0], Does.Contain("var (classArg1, classArg2, classArg3) = classArg0;"));
            
            Assert.That(generatedFiles[0], Does.Contain("var methodArg0 = global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.TupleMethod();"));
            Assert.That(generatedFiles[0], Does.Contain("var (methodArg1, methodArg2, methodArg3) = methodArg0;"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodArg1, methodArg2, methodArg3],"));
            Assert.That(generatedFiles[0], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg1}, {methodArg2}, {methodArg3})\","));
            Assert.That(generatedFiles[0], Does.Contain("TestBody = classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.DataSource_TupleMethod(methodArg1, methodArg2, methodArg3)),"));
        });
}