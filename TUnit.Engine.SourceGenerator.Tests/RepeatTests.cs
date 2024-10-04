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
            
            AssertFileContains(generatedFiles[0], "RepeatLimit = 1,");
            AssertFileContains(generatedFiles[1], "RepeatLimit = 1,");
            AssertFileContains(generatedFiles[2], "RepeatLimit = 2,");
            AssertFileContains(generatedFiles[3], "RepeatLimit = 2,");
            AssertFileContains(generatedFiles[4], "RepeatLimit = 2,");
            AssertFileContains(generatedFiles[5], "RepeatLimit = 3,");
            AssertFileContains(generatedFiles[6], "RepeatLimit = 3,");
            AssertFileContains(generatedFiles[7], "RepeatLimit = 3,");
            AssertFileContains(generatedFiles[8], "RepeatLimit = 3,");
            
            AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 0,");
            AssertFileContains(generatedFiles[1], "CurrentRepeatAttempt = 1,");
            AssertFileContains(generatedFiles[2], "CurrentRepeatAttempt = 0,");
            AssertFileContains(generatedFiles[3], "CurrentRepeatAttempt = 1,");
            AssertFileContains(generatedFiles[4], "CurrentRepeatAttempt = 2,");
            AssertFileContains(generatedFiles[5], "CurrentRepeatAttempt = 0,");
            AssertFileContains(generatedFiles[6], "CurrentRepeatAttempt = 1,");
            AssertFileContains(generatedFiles[7], "CurrentRepeatAttempt = 2,");
            AssertFileContains(generatedFiles[8], "CurrentRepeatAttempt = 3,");
        });
}