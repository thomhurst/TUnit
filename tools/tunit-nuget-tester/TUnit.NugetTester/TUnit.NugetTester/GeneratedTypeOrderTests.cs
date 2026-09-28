using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.NugetTester;

/// <summary>
/// TUnit.Core.props adds TUnit.Core.GeneratedNamespace.cs ahead of the project's own files so the compiler
/// emits the source-generated TUnit.Generated types after the user's types. Emitting them first made
/// Microsoft Defender scan large test assemblies for seconds on every build (see PR #6908).
/// This project consumes the packed packages, so it guards the real NuGet import path.
/// </summary>
public class GeneratedTypeOrderTests
{
    [Test]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only inspects this assembly's own type definitions.")]
    public async Task Generated_Types_Are_Emitted_After_User_Types()
    {
        var types = typeof(GeneratedTypeOrderTests).Assembly.GetTypes();

        var generatedTypes = types.Where(t => t.Namespace == "TUnit.Generated").ToArray();
        var userTypes = types.Where(t => !t.IsNested && t.Namespace == typeof(GeneratedTypeOrderTests).Namespace).ToArray();

        await Assert.That(generatedTypes).IsNotEmpty();
        await Assert.That(userTypes).IsNotEmpty();

        // TypeDef metadata tokens follow the order the compiler emitted the types in.
        var firstGenerated = generatedTypes.Min(t => t.MetadataToken);
        var lastUser = userTypes.Max(t => t.MetadataToken);

        await Assert.That(firstGenerated).IsGreaterThan(lastUser);
    }
}
