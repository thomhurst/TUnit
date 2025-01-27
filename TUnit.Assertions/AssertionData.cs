namespace TUnit.Assertions;

public readonly record struct AssertionData(object? Result, Exception? Exception, string? ActualExpression)
{
    public static implicit operator AssertionData((object?, Exception?, string?) tuple) =>
        new(tuple.Item1, tuple.Item2, tuple.Item3);
}