using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class MethodDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(1));
            
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg0 = global::TUnit.TestProject.MethodDataSourceDrivenTests.SomeMethod();"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Method(methodArg0)"));
        });
}