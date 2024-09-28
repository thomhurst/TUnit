using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class NumberArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NumberArgumentTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(6));
            
            Assert.That(generatedFiles[0], Does.Contain("global::System.Int32 methodArg0 = 1;"));
            Assert.That(generatedFiles[1], Does.Contain("global::System.Double methodArg0 = 1.1;"));
            Assert.That(generatedFiles[2], Does.Contain("global::System.Single methodArg0 = 1.1f;"));
            Assert.That(generatedFiles[3], Does.Contain("global::System.Int64 methodArg0 = 1L;"));
            Assert.That(generatedFiles[4], Does.Contain("global::System.UInt64 methodArg0 = 1UL;"));
            Assert.That(generatedFiles[5], Does.Contain("global::System.UInt32 methodArg0 = 1U;"));
        });

    [Test]
    [SetCulture("de-DE")]
    public Task TestDE() => Test();
}