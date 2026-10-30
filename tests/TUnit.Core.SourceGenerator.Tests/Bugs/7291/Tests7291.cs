namespace TUnit.Core.SourceGenerator.Tests.Bugs._7291;

/// <summary>
/// Snapshot of the property injection source for a fixture in namespace Core.Api.Tests.
/// The generated code lives in namespace TUnit.Core, so any type name it writes without
/// global:: binds to TUnit.Core.* and shows up here as an unqualified name (PR #7291).
/// </summary>
internal class Tests7291 : TestsBase
{
    [Test]
    public Task Test() => PropertyInjectionGenerator.RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "_7291",
            "Tests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });
}
