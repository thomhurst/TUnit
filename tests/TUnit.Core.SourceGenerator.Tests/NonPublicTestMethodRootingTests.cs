namespace TUnit.Core.SourceGenerator.Tests;

internal class NonPublicTestMethodRootingTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject",
            "NonPublicTestMethodRootingTests.cs"),
        async generatedFiles =>
        {
            var generated = string.Join(Environment.NewLine, generatedFiles);

            // Each non-public test method is rooted by its exact signature, never by name or by type annotation.
            await Assert.That(generated).Contains("DynamicDependency(\"Internal_Method(System.Int32,System.String)\"");
            await Assert.That(generated).Contains("DynamicDependency(\"Internal_Generic``1(``0)\"");
            await Assert.That(generated).Contains("DynamicDependency(\"Internal_Shares_Name(System.Int32)\"");
            await Assert.That(generated).Contains("DynamicDependency(\"Internal_Ref_Overload(System.Int32)\"");
            await Assert.That(generated).DoesNotContain(".GetMethod(\"Internal_");
        });
}
