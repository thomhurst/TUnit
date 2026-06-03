using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace TUnit.Tests.Shared;

/// <summary>
/// Verifies that a code fixer assembly carries no IL reference to its analyzer project's
/// <c>Rules</c> type. Guards against https://github.com/thomhurst/TUnit/issues/6157.
/// </summary>
/// <remarks>
/// Code fixer assemblies ship in the version-agnostic <c>analyzers/dotnet/cs</c> folder while the
/// analyzer assemblies ship per-Roslyn (<c>analyzers/dotnet/roslyn4.x/cs</c>), and the dependency
/// resolves at runtime by simple name. Visual Studio cannot unload analyzer assemblies, so after a
/// package update (or with mixed TUnit versions in one VS session) a new code fixer can bind
/// against a stale analyzer assembly. Any IL reference to the <c>Rules</c> type — e.g.
/// <c>Rules.X.Id</c> inside the eagerly-evaluated <c>FixableDiagnosticIds</c> — then throws
/// <see cref="System.MissingFieldException"/> for rules the stale assembly doesn't have. Code
/// fixers must use the compile-time-baked <c>DiagnosticIds</c> constants instead, which this
/// helper enforces at the IL level: a <c>TypeReference</c> to <c>Rules</c> appears for any usage
/// (field access, <c>typeof</c>, method call), so an empty result proves full decoupling.
/// <c>DiagnosticIds</c> itself is also scanned — its members must stay <c>const</c>; changing one
/// to <c>static readonly</c> would silently reintroduce a runtime type reference, which surfaces
/// here as a <c>TypeReference</c> to <c>DiagnosticIds</c>.
/// <para>
/// Linked into each code fixer test project via
/// <c>&lt;Compile Include="..\SharedTestHelpers\RulesDecouplingVerifier.cs"&gt;</c>.
/// </para>
/// </remarks>
internal static class RulesDecouplingVerifier
{
    /// <summary>
    /// Returns the fully-qualified names of all <c>Rules</c> or <c>DiagnosticIds</c> type
    /// references in <paramref name="codeFixersAssembly"/> whose namespace is
    /// <paramref name="rulesNamespace"/>. An empty list means the assembly is fully decoupled.
    /// </summary>
    public static List<string> FindRulesTypeReferences(Assembly codeFixersAssembly, string rulesNamespace)
    {
        using var stream = File.OpenRead(codeFixersAssembly.Location);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var rulesReferences = new List<string>();

        foreach (var handle in metadata.TypeReferences)
        {
            var typeReference = metadata.GetTypeReference(handle);
            var name = metadata.GetString(typeReference.Name);
            var typeNamespace = metadata.GetString(typeReference.Namespace);

            if (name is "Rules" or "DiagnosticIds" && typeNamespace == rulesNamespace)
            {
                rulesReferences.Add($"{typeNamespace}.{name}");
            }
        }

        return rulesReferences;
    }
}
