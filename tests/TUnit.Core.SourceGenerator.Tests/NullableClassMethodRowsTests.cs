namespace TUnit.Core.SourceGenerator.Tests;

internal class NullableClassMethodRowsTests : TestsBase
{
    [Test]
    public Task Generates_Nullable_Class_From_Method_Rows() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7573", "NullableClassMethodRows.cs"), async generatedFiles =>
    {
        foreach (var (method, closedType) in new[]
                 {
                     ("InfersBesideConstructor", "TypeFreeConstructorNullableRows<int>"),
                     ("UsesExternalProvider", "GenericProviderNullableRows<double>"),
                     ("OpenRow", "OpenClassMethodRows<int>"),
                     ("OpenArray", "OpenClassMethodRows<int>"),
                     ("ConstructorType", "ConstructorNullableClassRows<double>"),
                     ("SelectedOverload", "ArgumentOverloadClassRows<double>")
                 })
        {
            var generated = generatedFiles.Single(file => file.Contains($"\"{method}\""));
            await Assert.That(generated.Contains(closedType)).IsTrue();
            await Assert.That(generated.Contains("__mm_1")).IsFalse();
        }

        foreach (var method in new[] { "DirectArray", "FactoryArray" })
        {
            await Assert.That(generatedFiles.Any(file => file.Contains($"ArrayClassMethodRows_T_{method}_")
                && file.Contains("ArrayClassMethodRows<int>"))).IsTrue();
        }

        foreach (var method in new[] { "Value", "NullableValue", "Factory", "Tuple", "MultipleSources" })
        {
            await Assert.That(generatedFiles.Any(file => file.Contains($"NullableClassMethodRows_T_{method}_")
                && file.Contains("NullableClassMethodRows<int>"))).IsTrue();
        }
    });
}
