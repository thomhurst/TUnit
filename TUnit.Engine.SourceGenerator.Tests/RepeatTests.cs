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
            
            Assert.That(generatedFiles[0], Does.Contain("RepeatLimit = 1,"));
            Assert.That(generatedFiles[1], Does.Contain("RepeatLimit = 1,"));
            Assert.That(generatedFiles[2], Does.Contain("RepeatLimit = 2,"));
            Assert.That(generatedFiles[3], Does.Contain("RepeatLimit = 2,"));
            Assert.That(generatedFiles[4], Does.Contain("RepeatLimit = 2,"));
            Assert.That(generatedFiles[5], Does.Contain("RepeatLimit = 3,"));
            Assert.That(generatedFiles[6], Does.Contain("RepeatLimit = 3,"));
            Assert.That(generatedFiles[7], Does.Contain("RepeatLimit = 3,"));
            Assert.That(generatedFiles[8], Does.Contain("RepeatLimit = 3,"));
            
            Assert.That(generatedFiles[0], Does.Contain("CurrentRepeatAttempt = 0,"));
            Assert.That(generatedFiles[1], Does.Contain("CurrentRepeatAttempt = 1,"));
            Assert.That(generatedFiles[2], Does.Contain("CurrentRepeatAttempt = 0,"));
            Assert.That(generatedFiles[3], Does.Contain("CurrentRepeatAttempt = 1,"));
            Assert.That(generatedFiles[4], Does.Contain("CurrentRepeatAttempt = 2,"));
            Assert.That(generatedFiles[5], Does.Contain("CurrentRepeatAttempt = 0,"));
            Assert.That(generatedFiles[6], Does.Contain("CurrentRepeatAttempt = 1,"));
            Assert.That(generatedFiles[7], Does.Contain("CurrentRepeatAttempt = 2,"));
            Assert.That(generatedFiles[8], Does.Contain("CurrentRepeatAttempt = 3,"));
        });
}