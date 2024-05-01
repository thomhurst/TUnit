namespace TUnit.Engine.SourceGenerator.Tests;

public class ReturnTypeTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ReturnTypeTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(6));
            Assert.That(generatedFiles[0], Does.Contain("ReturnType = typeof(void),"));
            Assert.That(generatedFiles[1], Does.Contain("ReturnType = typeof(global::System.Int32),"));
            Assert.That(generatedFiles[2], Does.Contain("ReturnType = typeof(Task),"));
            Assert.That(generatedFiles[3], Does.Contain("ReturnType = typeof(Task<global::System.Int32>),"));
            Assert.That(generatedFiles[4], Does.Contain("ReturnType = typeof(ValueTask),"));
            Assert.That(generatedFiles[5], Does.Contain("ReturnType = typeof(ValueTask<global::System.Int32>),"));
        });
}