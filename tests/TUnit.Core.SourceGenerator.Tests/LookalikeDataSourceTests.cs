using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class LookalikeDataSourceTests : TestsBase
{
    [Test]
    public Task Metadata() => TestMetadataGenerator.RunTest(Fixture, _ => Task.CompletedTask);

    [Test]
    public Task Static_properties() => new TestsBase<StaticPropertyInitializationGenerator>()
        .RunTest(Fixture, _ => Task.CompletedTask);

    private static string Fixture => Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "LookalikeDataSourceTests.cs");
}
