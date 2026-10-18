using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Generic-method instantiations are named with C# keyword aliases and no namespaces
/// (<c>Generated&lt;List&lt;int&gt;&gt;</c>), as reflection mode names them (#7105). Their test ids
/// still use fully qualified type arguments, so same-named types from different namespaces stay distinct.
/// </summary>
internal class Tests7105 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7105",
        "GenericNameParityTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles = [Path.Combine(
                Git.TestsDirectory.FullName,
                "TUnit.TestProject",
                "Bugs",
                "_7105",
                "SameNamedPayloads.cs")],
        },
        async generatedFiles =>
        {
            var generated = string.Join(Environment.NewLine, generatedFiles);

            await Assert.That(generated).Contains("methodTypeArgumentsIdentity: \"<global::TUnit.TestProject.Bugs._7105.First.SamePayload>\"");
            await Assert.That(generated).Contains("methodTypeArgumentsIdentity: \"<global::TUnit.TestProject.Bugs._7105.Second.SamePayload>\"");
        });
}
