using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class CustomDisplayNameTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "CustomDisplayNameTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(4));
            
            Assert.That(generatedFiles[0], Does.Contain("DisplayName = $\"A super important test!\","));
            Assert.That(generatedFiles[1], Does.Contain("DisplayName = $\"Another super important test!\","));
            Assert.That(generatedFiles[2], Does.Contain("DisplayName = $\"Test with: {methodArg} {methodArg1} {methodArg2}!\","));
            Assert.That(generatedFiles[3], Does.Contain("DisplayName = $\"Test with: {methodArg} {methodArg1} {methodArg2}!\","));
        });
}