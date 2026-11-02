namespace TUnit.Core.SourceGenerator.Tests.Bugs._7225;

/// <summary>
/// [InstanceMethodDataSource] gets the same generated Factory as [MethodDataSource], so Native AOT
/// does not fall back to a reflection member lookup (#7225).
/// </summary>
internal class Tests7225 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7225",
            "InstanceMethodDataSourceFieldTests.cs"),
        async generatedFiles =>
        {
            var source = string.Join(Environment.NewLine, generatedFiles);
            const string testClass = "global::TUnit.TestProject.Bugs._7225.InstanceMethodDataSourceFieldTests";
            await Assert.That(source).Contains($"FromInstanceRuntimeValue(dataGeneratorMetadata, static instance => (object?)(({testClass})instance).Rows ?? global::System.Array.Empty<object>())");
            await Assert.That(source).Contains($"FromInstanceEnumerable(dataGeneratorMetadata, static instance => (object?)(({testClass})instance).PropertyRows ?? global::System.Array.Empty<object>())");
            await Assert.That(source).Contains($"FromEnumerable(static () => (object?){testClass}.StaticRows() ?? global::System.Array.Empty<object>())");
            await Assert.That(source).DoesNotContain("new global::TUnit.Core.InstanceMethodDataSourceAttribute(\"Rows\"),");
        });

    /// <summary>
    /// The factory keeps the behavior [InstanceMethodDataSource] had when it read members by reflection: an object
    /// value decides its rows at run time, a null value is an empty source, and a private method is looked up
    /// by a constant name that Native AOT keeps, as private fields and properties are.
    /// </summary>
    [Test]
    public Task MemberShapes() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7225",
            "InstanceMethodDataSourceMemberShapeTests.cs"),
        async generatedFiles =>
        {
            var source = string.Join(Environment.NewLine, generatedFiles);
            const string testClass = "global::TUnit.TestProject.Bugs._7225.InstanceMethodDataSourceMemberShapeTests";
            await Assert.That(source).Contains($"FromInstanceRuntimeValue(dataGeneratorMetadata, static instance => (object?)(({testClass})instance).ObjectProperty ?? global::System.Array.Empty<object>())");
            await Assert.That(source).Contains($"FromInstanceRuntimeValue(dataGeneratorMetadata, static instance => (object?)(({testClass})instance).ObjectMethod() ?? global::System.Array.Empty<object>())");
            await Assert.That(source).Contains($"FromInstanceEnumerable(dataGeneratorMetadata, static instance => (object?)(({testClass})instance).TupleRows ?? global::System.Array.Empty<object>())");
            await Assert.That(source).Contains($"FromInstanceEnumerable(dataGeneratorMetadata, static instance => typeof({testClass}).GetMethod(\"PrivateRows\", ");
            await Assert.That(source).DoesNotContain("new global::TUnit.Core.InstanceMethodDataSourceAttribute(\"PrivateRows\"),");
            // A generic overload of the same name would make the GetMethod lookup ambiguous
            await Assert.That(source).Contains($"global::System.Array.Find(typeof({testClass}).GetMethods(");
            await Assert.That(source).Contains("m.Name == \"OverloadedRows\" && !m.IsGenericMethodDefinition && m.GetParameters().Length == 0)!.Invoke(instance, null)");
        });
}
