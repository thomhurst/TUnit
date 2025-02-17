using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record SourceGeneratedPropertyInformation : SourceGeneratedMemberInformation
{
    public override required Type Type { get; init; }
    public required bool IsStatic { get; init; }
}