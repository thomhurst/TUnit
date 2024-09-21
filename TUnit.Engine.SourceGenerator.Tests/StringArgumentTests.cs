using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class StringArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "StringArgumentTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles[0], Does.Contain(
                """
                global::System.String methodArg0 = "";
                """));
            
            Assert.That(generatedFiles[1], Does.Contain(
                """
                global::System.String methodArg0 = "\\";
                """));
            
            Assert.That(generatedFiles[2], Does.Contain(
                """
                global::System.String methodArg0 = "\\t";
                """));
            
            Assert.That(generatedFiles[3], Does.Contain(
                """
                global::System.String methodArg0 = " ";
                """));
            
            Assert.That(generatedFiles[4], Does.Contain(
                """
                global::System.String methodArg0 = "\\t";
                """));
            
            Assert.That(generatedFiles[5], Does.Contain(
                """
                global::System.String methodArg0 = "\\ ";
                """));
            
            Assert.That(generatedFiles[6], Does.Contain(
                """
                global::System.String methodArg0 = "\\";
                """));
        });
}