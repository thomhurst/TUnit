using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class LookalikeDataSourceTests : TestsBase
{
    [Test]
    public Task Metadata() => TestMetadataGenerator.RunTest(Fixture, _ => Task.CompletedTask);

    [Test]
    public Task Static_properties() => new TestsBase<StaticPropertyInitializationGenerator>()
        .RunTest(Fixture, _ => Task.CompletedTask);

    // Verify row scoping independently of the runtime's custom-attribute reflection fallback.
    [Test]
    public Task Derived_class_method_sources() => TestMetadataGenerator.RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.Core.SourceGenerator.Tests", "LookalikeClassMethodSources.cs.txt"),
        _ => Task.CompletedTask);

    [Test]
    public Task Reimplemented_rows() => TestMetadataGenerator.RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "ReimplementedMethodSourceTests.cs"),
        _ => Task.CompletedTask);

    [Test]
    public Task Deferred_derived_static_rows() => TestMetadataGenerator.RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "DerivedSourceDeferredRowsTests.cs"),
        _ => Task.CompletedTask);

    private static string Fixture => Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "LookalikeDataSourceTests.cs");
}
