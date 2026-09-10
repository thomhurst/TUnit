using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Mocks.SourceGenerator.Models;

/// <summary>
/// Pairs an equatable mock model with the source request that triggered it. Location data stays
/// primitive so incremental state never retains a syntax tree.
/// </summary>
internal readonly record struct MockGenerationRequest(MockTypeModel Model, MockSourceLocation SourceLocation);

internal readonly record struct MockSourceLocation(
    string FilePath,
    int SpanStart,
    int SpanLength,
    int StartLine,
    int StartCharacter,
    int EndLine,
    int EndCharacter)
{
    public static MockSourceLocation From(Location location)
    {
        var lineSpan = location.GetLineSpan();
        return new MockSourceLocation(
            lineSpan.Path,
            location.SourceSpan.Start,
            location.SourceSpan.Length,
            lineSpan.StartLinePosition.Line,
            lineSpan.StartLinePosition.Character,
            lineSpan.EndLinePosition.Line,
            lineSpan.EndLinePosition.Character);
    }

    public Location ToLocation() => Location.Create(
        FilePath,
        new TextSpan(SpanStart, SpanLength),
        new LinePositionSpan(
            new LinePosition(StartLine, StartCharacter),
            new LinePosition(EndLine, EndCharacter)));
}
