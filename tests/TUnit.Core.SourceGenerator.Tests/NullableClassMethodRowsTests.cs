namespace TUnit.Core.SourceGenerator.Tests;

internal class NullableClassMethodRowsTests : TestsBase
{
    [Test]
    public Task Generates_Nullable_Class_From_Method_Rows() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7573", "NullableClassMethodRows.cs"), async generatedFiles =>
    {
        foreach (var (method, closedType) in new[]
                 {
                     ("RetainsInstanceBesideConstructor", "TypeFreeConstructorUninferredRows<int>"),
                     ("RetainsUntypedSource", "ExplicitUntypedClassRows<int>"),
                     ("InfersBesideConstructor", "TypeFreeConstructorNullableRows<int>"),
                     ("UsesExternalProvider", "GenericProviderNullableRows<double>"),
                     ("OpenRow", "OpenClassMethodRows<int>"),
                     ("OpenArray", "OpenClassMethodRows<int>"),
                     ("ConstructorType", "ConstructorNullableClassRows<double>"),
                     ("NestedConstructorType", "ConstructorNestedNullableClassRows<double>"),
                     ("RetainsConstructorValueFallback", "PartialConstructorValueFallback<string, int>"),
                     ("LastOccurrenceInfersClassType", "RepeatedNullableClassParameter<long>"),
                     ("InfersThroughNullableConstructedStruct", "NullableConstructedStructArrayRows<string>"),
                     ("PartialConstructorTypes", "PartialConstructorNullableClassRows<int, string>"),
                     ("SelectedOverload", "ArgumentOverloadClassRows<double>")
                 })
        {
            var generated = generatedFiles.Single(file => file.Contains($"\"{method}\""));
            await Assert.That(generated.Contains(closedType)).IsTrue();
            await Assert.That(generated.Contains("__mm_1")).IsFalse();
        }

        var partitionedSources = generatedFiles.Single(file => file.Contains("\"PartitionsBesideLiteral\""));
        var intSources = partitionedSources.Split("testDataSources:")[1].Split("classDataSources:")[0];
        await Assert.That(intSources.Contains(".IntRows()")).IsTrue();
        await Assert.That(intSources.Contains(".DoubleRows()")).IsFalse();
        var doubleSources = partitionedSources.Split("testDataSources:")[2].Split("classDataSources:")[0];
        await Assert.That(doubleSources.Contains(".DoubleRows()")).IsTrue();
        await Assert.That(doubleSources.Contains(".IntRows()")).IsFalse();

        var constructorSources = generatedFiles.Single(file => file.Contains("\"RetainsInstanceBesideConstructor\""));
        await Assert.That(constructorSources.Contains(".InstanceRows()")).IsTrue();
        var untypedSources = generatedFiles.Single(file => file.Contains("\"RetainsUntypedSource\""));
        await Assert.That(untypedSources.Split("testDataSources:")[1].Split("classDataSources:")[0].Contains("new global::TUnit.TestProject.Bugs._7573.AdditionalUntypedRowAttribute()")).IsTrue();

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
