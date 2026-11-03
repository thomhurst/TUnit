namespace TUnit.Core.SourceGenerator.Tests;

internal class AssignableTypedRowTests : TestsBase
{
    [Test]
    public Task Interfaces_and_base_types_emit_closed_tests() => RunTest(
        Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "Bugs", "_7466", "AssignableTypedRowTests.cs"),
        async files =>
        {
            var generated = string.Join(Environment.NewLine, files);
            await Assert.That(generated).Contains("instance.Array<int>(");
            await Assert.That(generated).Contains("instance.InheritedShapes<int>(");
        });
}
