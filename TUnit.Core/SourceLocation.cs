namespace TUnit.Core;

public record SourceLocation(string RawSource, string? FileName, int MinLineNumber, int MaxLineNumber);