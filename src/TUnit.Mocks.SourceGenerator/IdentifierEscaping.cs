using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator;

/// <summary>
/// Utility for escaping C# identifiers that collide with reserved keywords.
/// Used by all builders at member-name emission sites so that types declaring
/// members like <c>@class</c>, <c>@event</c>, <c>@record</c> compile.
/// <para>
/// IMPORTANT: Stored model <c>Name</c> values must remain UNESCAPED — they are
/// used for engine dispatch keys, logging, and identity. Only escape at the
/// point where the name becomes a C# identifier in the generated source.
/// </para>
/// </summary>
internal static class IdentifierEscaping
{
    /// <summary>
    /// Returns <paramref name="name"/> prefixed with <c>@</c> when it is a C# reserved keyword,
    /// otherwise returns <paramref name="name"/> unchanged.
    /// E.g., <c>"event"</c> → <c>"@event"</c>, <c>"class"</c> → <c>"@class"</c>, <c>"Foo"</c> → <c>"Foo"</c>.
    /// </summary>
    // WHY only GetKeywordKind (reserved): contextual keywords like `record`, `async`, `var`,
    // `get`, `set`, `nameof`, `value` are valid identifiers and must NOT be `@`-escaped — the
    // C# compiler already disambiguates them by position. Escaping a contextual keyword would
    // change the identifier's textual form (e.g. `record` → `@record`), breaking explicit
    // interface implementation matching against the source-declared member name.
    internal static string EscapeIdentifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
}
