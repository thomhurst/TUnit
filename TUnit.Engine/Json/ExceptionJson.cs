namespace TUnit.Engine.Json;

public record ExceptionJson
{
    public required string? Type { get; init; }
    public required string Message { get; init; }
    public required string? Stacktrace { get; init; }
    public required ExceptionJson? InnerException { get; init; }
}
