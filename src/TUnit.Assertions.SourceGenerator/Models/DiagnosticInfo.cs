using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Assertions.SourceGenerator.Models;

/// <summary>
/// Equatable stand-in for a <see cref="Diagnostic"/>. A <see cref="Diagnostic"/> holds a
/// <see cref="Location"/> that references its <see cref="SyntaxTree"/>, which would keep old
/// compilations alive in the incremental cache; this captures only primitive location data.
/// </summary>
internal sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    string? FilePath,
    string? SourceChecksum,
    TextSpan TextSpan,
    LinePositionSpan LineSpan,
    ImmutableEquatableArray<string> MessageArgs)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params string[] messageArgs)
    {
        var lineSpan = location.GetLineSpan();
        return new DiagnosticInfo(
            descriptor,
            lineSpan.Path,
            location.SourceTree is { } tree ? GetChecksum(tree) : null,
            location.SourceSpan,
            lineSpan.Span,
            messageArgs.ToImmutableEquatableArray());
    }

    private static string GetChecksum(SyntaxTree tree)
        => Convert.ToBase64String(tree.GetText().GetChecksum().ToArray());

    public Diagnostic ToDiagnostic(Compilation compilation)
    {
        if (FilePath is null)
        {
            return Diagnostic.Create(Descriptor, Location.None, MessageArgs.Cast<object?>().ToArray());
        }

        // Reattach to the tree with the same path. Paths need not be unique (trees parsed without
        // a path all share an empty one), so when several match, the text checksum picks the tree.
        SyntaxTree? match = null;
        var ambiguous = false;
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (!string.Equals(tree.FilePath, FilePath, StringComparison.Ordinal))
            {
                continue;
            }

            if (match is null)
            {
                match = tree;
            }
            else
            {
                ambiguous = true;
                break;
            }
        }

        if (ambiguous)
        {
            match = null;
            if (SourceChecksum is not null)
            {
                foreach (var tree in compilation.SyntaxTrees)
                {
                    if (string.Equals(tree.FilePath, FilePath, StringComparison.Ordinal)
                        && string.Equals(GetChecksum(tree), SourceChecksum, StringComparison.Ordinal))
                    {
                        match = tree;
                        break;
                    }
                }
            }
        }

        var location = match is not null && TextSpan.End <= match.Length
            ? Location.Create(match, TextSpan)
            : Location.Create(FilePath, TextSpan, LineSpan);

        return Diagnostic.Create(Descriptor, location, MessageArgs.Cast<object?>().ToArray());
    }
}
