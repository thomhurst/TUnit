namespace TUnit.Engine.Json;

public record TestJson
{
    public required string TestId { get; init; }

    public required string TestName { get; init; }
    public required string DisplayName { get; set; }

    public required string? ClassType { get; init; }

    public required string?[]? TestMethodParameterTypes { get; init; }
    public required object?[]? TestMethodArguments { get; init; }

    public required string?[]? TestClassParameterTypes { get; init; }
    public required object?[]? TestClassArguments { get; init; }
    //
    public required IReadOnlyList<string> Categories { get; init; }

    public required int RetryLimit { get; init; }

    public required TimeSpan? Timeout { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<string>> CustomProperties { get; init; }

    public required string? ReturnType { get; init; }

    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }

    public required Dictionary<string, object?> ObjectBag { get; init; }

    public required TestResultJson? Result { get; set; }
}
