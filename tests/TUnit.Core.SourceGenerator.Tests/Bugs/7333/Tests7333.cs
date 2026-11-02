namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7333 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7333", "ClassMethodDataSourceInferenceTests.cs"),
        async generatedFiles =>
        {
            foreach (var className in new[] { "ExternalRows<int>", "OwnRows<int>", "AsyncFactoryRows<string>", "MultipleSources<int>", "GenericMethod<int>", "MethodRows<int>" })
            {
                await Assert.That(generatedFiles.Any(file => file.Contains($"global::TUnit.TestProject.Bugs._7333.{className}"))).IsTrue();
            }
            await Assert.That(generatedFiles.Any(file => file.Contains("Could not infer the type arguments of generic class 'UninferableOwnRows`1'"))).IsTrue();
        });
}
