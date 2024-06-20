using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class RepeatTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "RepeatTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(9));
            
            Assert.That(generatedFiles[0], Does.Contain("RepeatCount = 1,"));
            Assert.That(generatedFiles[1], Does.Contain("RepeatCount = 1,"));
            Assert.That(generatedFiles[2], Does.Contain("RepeatCount = 2,"));
            Assert.That(generatedFiles[3], Does.Contain("RepeatCount = 2,"));
            Assert.That(generatedFiles[4], Does.Contain("RepeatCount = 2,"));
            Assert.That(generatedFiles[5], Does.Contain("RepeatCount = 3,"));
            Assert.That(generatedFiles[6], Does.Contain("RepeatCount = 3,"));
            Assert.That(generatedFiles[7], Does.Contain("RepeatCount = 3,"));
            Assert.That(generatedFiles[8], Does.Contain("RepeatCount = 3,"));
            
            Assert.That(generatedFiles[0], Does.Contain("RepeatIndex = 0,"));
            Assert.That(generatedFiles[1], Does.Contain("RepeatIndex = 1,"));
            Assert.That(generatedFiles[2], Does.Contain("RepeatIndex = 0,"));
            Assert.That(generatedFiles[3], Does.Contain("RepeatIndex = 1,"));
            Assert.That(generatedFiles[4], Does.Contain("RepeatIndex = 2,"));
            Assert.That(generatedFiles[5], Does.Contain("RepeatIndex = 0,"));
            Assert.That(generatedFiles[6], Does.Contain("RepeatIndex = 1,"));
            Assert.That(generatedFiles[7], Does.Contain("RepeatIndex = 2,"));
            Assert.That(generatedFiles[8], Does.Contain("RepeatIndex = 3,"));
        });
}