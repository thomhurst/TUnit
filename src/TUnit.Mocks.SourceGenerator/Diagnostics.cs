using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator;

/// <summary>
/// Diagnostics reported by the generator itself. Everything the analyzer can see at a call site
/// belongs in TUnit.Mocks.Analyzers (TM001-TM007); this file is for failures only the generator
/// can observe, such as whole-compilation name collisions and unexpected generation failures.
/// </summary>
internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor TM008_GeneratedNameCollision = new(
        id: "TM008",
        title: "Mocked types produce the same generated name",
        messageFormat: "Cannot mock '{0}' because it produces the same generated name '{1}' as '{2}'. Rename one of the types or namespaces.",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Generated type and file names are derived from the mocked type's fully qualified name with separators replaced by underscores. Two types can still map to the same name when their namespaces differ only in how underscores and dots are arranged (e.g. 'A_.B.IFoo' and 'A._B.IFoo'). Emitting both would give Roslyn duplicate hint names, which discards every mock in the compilation without saying why, so generation is skipped for the colliding types and reported here instead."
    );

    public static readonly DiagnosticDescriptor TM009_GenerationFailed = new(
        id: "TM009",
        title: "Mock generation failed",
        messageFormat: "Failed to generate mock for '{0}': {1}: {2}",
        category: "TUnit.Mocks",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An unexpected exception prevented TUnit.Mocks from generating a requested mock. The exception type and message identify the failing generator path."
    );
}
