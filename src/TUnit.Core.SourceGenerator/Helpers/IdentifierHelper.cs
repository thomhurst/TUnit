using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Core.SourceGenerator.Helpers;

internal static class IdentifierHelper
{
    /// <summary>
    /// A symbol's Name drops the @ of an escaped identifier, so a member named after a keyword, such as
    /// <c>@event</c>, needs it back before generated code can access the member.
    /// </summary>
    public static string Escape(string name)
        => SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
}
