using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Tests to verify that generated code compiles correctly when multiple assemblies
/// define types with the same fully-qualified name.
/// See: https://github.com/thomhurst/TUnit/issues/4663
/// </summary>
internal class DuplicateTypeNameAcrossAssembliesTests : TestsBase
{
    /// <summary>
    /// This test verifies that the InfrastructureGenerator correctly handles the case
    /// where a type name is ambiguous (exists in multiple places).
    ///
    /// We simulate this by adding a synthetic type with the same fully-qualified name as
    /// a type from the VerifyTUnit package. The generator uses GetTypeByMetadataName() to
    /// detect ambiguous types and skip them, finding a unique type instead.
    ///
    /// Without the fix: generator picks DanglingSnapshots, which is ambiguous
    /// With the fix: generator detects ambiguity and picks DerivePathInfo instead
    /// </summary>
    [Test]
    public Task InfrastructureGenerator_WithDuplicateTypeNames_CompilesSuccessfully() => InfrastructureGenerator.RunTest(
        Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "BasicTests.cs"),
        new RunTestOptions
        {
            AdditionalSyntaxes =
            [
                // Create a type that conflicts with VerifyTUnit.DanglingSnapshots
                // This simulates the scenario where two assemblies define the same type
                """
                namespace VerifyTUnit
                {
                    public class DanglingSnapshots { }
                }
                """
            ],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            // If we get here without compilation errors, the test passes
            // The fix ensures the generator picks unique types that don't conflict
            await Assert.That(generatedFiles).IsNotEmpty();

            // Verify that the generated code doesn't reference the conflicting type
            var infrastructureFile = generatedFiles.FirstOrDefault(f => f.Contains("TUnitInfrastructure"));
            await Assert.That(infrastructureFile).IsNotNull();

            // The generator should have found a different type from VerifyTUnit assembly
            // since DanglingSnapshots is now ambiguous
            await Assert.That(infrastructureFile!).DoesNotContain("typeof(global::VerifyTUnit.DanglingSnapshots)");
        });
}
