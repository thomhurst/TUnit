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
            
            AssertFileContains(generatedFiles[0], "DisplayName = $\"A super important test!\",");
            AssertFileContains(generatedFiles[1], "DisplayName = $\"Another super important test!\",");
            AssertFileContains(generatedFiles[2], "DisplayName = $\"Test with: {methodArg} {methodArg1} {methodArg2}!\",");
            AssertFileContains(generatedFiles[3], "DisplayName = $\"Test with: {methodArg} {methodArg1} {methodArg2}!\",");
        });
}