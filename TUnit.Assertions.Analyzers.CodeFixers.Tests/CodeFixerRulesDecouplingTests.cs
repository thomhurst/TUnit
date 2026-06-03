using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace TUnit.Assertions.Analyzers.CodeFixers.Tests;

/// <summary>
/// Guards against https://github.com/thomhurst/TUnit/issues/6157.
///
/// TUnit.Assertions.Analyzers.CodeFixers.dll ships in the version-agnostic analyzers/dotnet/cs
/// folder and resolves its TUnit.Assertions.Analyzers dependency at runtime by simple name.
/// Visual Studio cannot unload analyzer assemblies, so after a package update the new code fixers
/// can bind against a stale TUnit.Assertions.Analyzers.dll. Any IL reference to the <c>Rules</c>
/// type inside the eagerly-evaluated <c>FixableDiagnosticIds</c> then throws
/// MissingFieldException for rules the stale assembly doesn't have. Code fixers must use the
/// compile-time-baked <c>DiagnosticIds</c> constants instead.
/// </summary>
public class CodeFixerRulesDecouplingTests
{
    [Test]
    public async Task CodeFixers_Assembly_Has_No_Reference_To_Rules_Type()
    {
        var assemblyPath = typeof(AwaitAssertionCodeFixProvider).Assembly.Location;

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var rulesReferences = new List<string>();

        foreach (var handle in metadata.TypeReferences)
        {
            var typeReference = metadata.GetTypeReference(handle);

            if (metadata.GetString(typeReference.Name) == "Rules" &&
                metadata.GetString(typeReference.Namespace) == "TUnit.Assertions.Analyzers")
            {
                rulesReferences.Add($"{metadata.GetString(typeReference.Namespace)}.{metadata.GetString(typeReference.Name)}");
            }
        }

        await Assert.That(rulesReferences)
            .IsEmpty()
            .Because("TUnit.Assertions.Analyzers.CodeFixers must not reference TUnit.Assertions.Analyzers.Rules at runtime - " +
                     "use DiagnosticIds constants instead (see issue #6157)");
    }
}
