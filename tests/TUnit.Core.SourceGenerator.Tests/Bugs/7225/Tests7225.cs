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
            await Assert.That(source).Contains($"FromInstanceRuntimeValue(dataGeneratorMetadata, static instance => (({testClass})instance).Rows)");
            await Assert.That(source).Contains($"FromInstanceEnumerable(dataGeneratorMetadata, static instance => (({testClass})instance).PropertyRows)");
            await Assert.That(source).Contains($"FromEnumerable(static () => {testClass}.StaticRows())");
            await Assert.That(source).DoesNotContain("new global::TUnit.Core.InstanceMethodDataSourceAttribute(\"Rows\"),");
        });

    /// <summary>
    /// The factory keeps the behavior [InstanceMethodDataSource] had when it read members by reflection: an object
    /// value decides its rows at run time, and a private method is looked up
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
            await Assert.That(source).Contains($"FromInstanceRuntimeValue(dataGeneratorMetadata, static instance => (({testClass})instance).ObjectProperty)");
            await Assert.That(source).Contains($"FromInstanceRuntimeValue(dataGeneratorMetadata, static instance => (({testClass})instance).ObjectMethod())");
            await Assert.That(source).Contains($"FromInstanceEnumerable(dataGeneratorMetadata, static instance => (({testClass})instance).TupleRows)");
            await Assert.That(source).Contains($"FromInstanceEnumerable(dataGeneratorMetadata, static instance => typeof({testClass}).GetMethod(\"PrivateRows\", ");
            await Assert.That(source).DoesNotContain("new global::TUnit.Core.InstanceMethodDataSourceAttribute(\"PrivateRows\"),");
        });
}
