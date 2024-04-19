namespace TUnit.Engine.SourceGenerator.Tests;

public class DataSourceDrivenTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DataSourceDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("var methodArg0 = SomeMethod();"));
            Assert.That(generatedFiles[0], Does.Contain("classInstance.DataSource_Method(methodArg0)"));
            
            Assert.That(generatedFiles[1], Does.Contain("global::TUnit.TestProject.DataSourceDrivenTests.SomeClass methodArg0 = new global::TUnit.TestProject.DataSourceDrivenTests.SomeClass();"));
            Assert.That(generatedFiles[1], Does.Contain("classInstance.DataSource_Class(methodArg0)"));
        });
}