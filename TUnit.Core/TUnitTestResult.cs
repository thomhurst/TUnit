namespace TUnit.Core;

public record TUnitTestResult
{
    public required Status Status { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
    public required TimeSpan Duration { get; init; }
    public required Exception? Exception { get; init; }
    public required string ComputerName { get; init; }
    public string? Output { get; internal set; }
};