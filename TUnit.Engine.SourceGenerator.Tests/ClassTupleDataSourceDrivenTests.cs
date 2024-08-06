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
            
            Assert.That(generatedFiles[0], Does.Contain("var (classArg0, classArg1, classArg2) = global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.TupleMethod();"));
            
            Assert.That(generatedFiles[0], Does.Contain("var (methodArg0, methodArg1, methodArg2) = global::TUnit.TestProject.ClassTupleDataSourceDrivenTests.TupleMethod();"));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodArguments = [methodArg0, methodArg1, methodArg2],"));
            Assert.That(generatedFiles[0], Does.Contain("DisplayName = $\"DataSource_TupleMethod({methodArg0}, {methodArg1}, {methodArg2})\","));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => AsyncConvert.Convert(() => classInstance.DataSource_TupleMethod(methodArg0, methodArg1, methodArg2))"));
        });
}