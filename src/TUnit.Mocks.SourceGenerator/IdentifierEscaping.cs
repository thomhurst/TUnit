using System.Text;
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

    /// <summary>
    /// Turns a type name (fully qualified, or a bare name with generic arguments) into a single
    /// C# identifier, used for generated type names and <c>AddSource</c> hint names.
    /// <para>
    /// The mapping must be injective: a hint-name clash makes Roslyn drop the generated sources
    /// for <em>both</em> types with no diagnostic, so the user only ever sees the downstream
    /// CS1061/CS0117 from the missing surface. Every literal <c>_</c> is therefore doubled before
    /// separators become <c>_</c>, so a single underscore in the result always came from a
    /// separator. Without that, <c>A_B.IFoo</c> and <c>A.B.IFoo</c> both produced
    /// <c>A_B_IFoo</c> — see issue #6505.
    /// </para>
    /// </summary>
    internal static string SanitizeIdentifier(string name)
    {
        name = name.Replace("global::", "");

        var sb = new StringBuilder(name.Length);
        var lastWasSeparator = false;

        foreach (var c in name)
        {
            if (c == ' ')
            {
                continue;
            }

            if (c == '_')
            {
                sb.Append("__");
                lastWasSeparator = false;
            }
            else if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator)
            {
                // Runs of separators still collapse to one '_' ("IFoo<T>" -> "IFoo_T_"); only the
                // separator/underscore distinction has to survive.
                sb.Append('_');
                lastWasSeparator = true;
            }
        }

        return sb.ToString();
    }
}
