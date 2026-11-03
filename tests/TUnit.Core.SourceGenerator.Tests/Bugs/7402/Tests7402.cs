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
}
