namespace TUnit.Core.SourceGenerator.Tests;

internal class GenericPropertyMetadataTests : TestsBase
{
    [Test]
    public Task GeneratesCachedAotCompatibleGetters() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "Bugs", "_7499", "GenericPropertyMetadataTests.cs"),
        async files =>
        {
            var output = string.Join("\n", files);
            await Assert.That(output).DoesNotContain("((dynamic)o)");
            await Assert.That(output).Contains("PropertyMetadataFactory.CreateGenericGetter");
        });
}
