using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class PriorityFilteringTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "PriorityFilteringTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = 
                [
                    Path.Combine(Git.RootDirectory.FullName,
                        "TUnit.TestProject",
                        "Enums",
                        "PriorityLevel.cs")
                ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(6));
            
            Assert.That(generatedFiles[0], Does.Contain("RepeatIndex = 1,"));
            Assert.That(generatedFiles[1], Does.Contain("RepeatIndex = 1,"));
            Assert.That(generatedFiles[2], Does.Contain("RepeatIndex = 2,"));
            Assert.That(generatedFiles[3], Does.Contain("RepeatIndex = 2,"));
            Assert.That(generatedFiles[4], Does.Contain("RepeatIndex = 2,"));
            Assert.That(generatedFiles[5], Does.Contain("RepeatIndex = 3,"));
            Assert.That(generatedFiles[6], Does.Contain("RepeatIndex = 3,"));
            Assert.That(generatedFiles[7], Does.Contain("RepeatIndex = 3,"));
            Assert.That(generatedFiles[8], Does.Contain("RepeatIndex = 3,"));
        });
}