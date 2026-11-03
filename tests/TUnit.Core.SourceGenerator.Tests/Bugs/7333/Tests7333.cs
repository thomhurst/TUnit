namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7333 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7333", "ClassMethodDataSourceInferenceTests.cs"),
        async generatedFiles =>
        {
            foreach (var className in new[] { "ExternalRows<int>", "OwnRows<int>", "AsyncFactoryRows<string>", "MultipleSources<int>", "GenericMethod<int>", "MethodRows<int>", "ClassAndMethodSources<int>", "PolymorphicExternalRows<int>", "ExternalArrayRows<int[]>", "ExternalSealedArrayRows<string[]>", "ExternalNestedArrayRows<int[][]>", "CovariantExternalArrayRows<string[]>", "NullableExternalRows<int>", "ExternalNullableArrayRows<int?[]>" })
            {
                await Assert.That(generatedFiles.Any(file => file.Contains($"global::TUnit.TestProject.Bugs._7333.{className}"))).IsTrue();
            }
            await Assert.That(generatedFiles.Any(file => file.Contains("global::TUnit.TestProject.Bugs._7333.UnmatchedMethodRows<int>"))).IsFalse();
            await Assert.That(generatedFiles.Any(file => file.Contains("global::TUnit.TestProject.Bugs._7333.PolymorphicExternalRows<global::System.IComparable>"))).IsFalse();
            await Assert.That(generatedFiles.Any(file => file.Contains("global::TUnit.TestProject.Bugs._7333.CovariantExternalArrayRows<object[]>"))).IsFalse();
            await Assert.That(generatedFiles.Any(file => file.Contains("global::TUnit.TestProject.Bugs._7333.NullableExternalRows<int?>"))).IsFalse();
            await Assert.That(generatedFiles.Any(file => file.Contains("Could not infer the type arguments of generic class 'UninferableOwnRows`1'"))).IsTrue();
        });
}
