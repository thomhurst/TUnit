namespace TUnit.Assertions;

public readonly record struct AssertionData(object? Result, Exception? Exception, string? ActualExpression, DateTimeOffset Start, DateTimeOffset End)
{
    public static implicit operator AssertionData((object?, Exception?, string?, DateTimeOffset, DateTimeOffset) tuple) =>
        new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5);
}
