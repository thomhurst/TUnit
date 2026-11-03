namespace TUnit.Core.SourceGenerator.Tests.Bugs._7402;

internal class Tests7402 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7402", "StaticTupleDataSourceTests.cs"),
        async generatedFiles =>
        {
            var source = string.Join(Environment.NewLine, generatedFiles);
            foreach (var helper in new[] { "Value", "Enumerable", "Task", "ValueTask", "AsyncEnumerable" })
            {
                await Assert.That(source).Contains($"DataSourceFactories.From{helper}(dataGeneratorMetadata, static () =>");
            }
        });

    [Test]
    public Task Generic_Tuples_Use_Shared_Binder() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7402", "GenericMutableTupleInitializerTests.cs"),
        async generatedFiles =>
        {
            var source = string.Join(Environment.NewLine, generatedFiles);
            await Assert.That(source).Contains("// Special handling for single reference tuple parameter");
            await Assert.That(source).Contains("new global::System.Tuple<");
        });
}
