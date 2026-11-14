namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7648 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName, "TUnit.TestProject", "Bugs", "_7648", "MethodSourceIdentityTests.cs"),
        async generatedFiles =>
        {
            var generated = string.Join("\n", generatedFiles);
            await Assert.That(generated).DoesNotContain("StaticSource<string>");
            await Assert.That(generated).DoesNotContain("InstanceSource<string>");
            await Assert.That(generated).Contains("StaticSource<int>");
            await Assert.That(generated).Contains("InstanceSource<int>");
        });
}
