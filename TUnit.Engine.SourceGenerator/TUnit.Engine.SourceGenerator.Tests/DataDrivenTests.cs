using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class DataDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DataDrivenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(6));
            
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg0 = 1;"));
            Assert.That(generatedFiles[1], Does.Contain("global::System.Int32 methodArg0 = 2;"));
            Assert.That(generatedFiles[2], Does.Contain("global::System.Int32 methodArg0 = 3;"));
            
            Assert.That(generatedFiles[3], Does.Contain("global::System.Int32 methodArg0 = 1;"));
            Assert.That(generatedFiles[3], Does.Contain("global::System.String methodArg1 = \"String\";"));            
            Assert.That(generatedFiles[4], Does.Contain("global::System.Int32 methodArg0 = 2;"));
            Assert.That(generatedFiles[4], Does.Contain("global::System.String methodArg1 = \"String2\";"));            
            Assert.That(generatedFiles[5], Does.Contain("global::System.Int32 methodArg0 = 3;"));
            Assert.That(generatedFiles[5], Does.Contain("global::System.String methodArg1 = \"String3\";"));
        });
}