using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class RepeatTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "RepeatTests.cs"),
        async generatedFiles =>
        {
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 1,");
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 1,");
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 2,");
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 2,");
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 2,");
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 3,");
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 3,");
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 3,");
            await AssertFileContains(generatedFiles[0], "RepeatLimit = 3,");
            
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 0,");
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 1,");
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 0,");
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 1,");
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 2,");
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 0,");
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 1,");
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 2,");
            await AssertFileContains(generatedFiles[0], "CurrentRepeatAttempt = 3,");
        });
}