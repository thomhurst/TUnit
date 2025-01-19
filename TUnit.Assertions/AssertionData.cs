namespace TUnit.Assertions;

public readonly record struct AssertionData<T>(T? Result, Exception? Exception, string? ActualExpression)
{
    public static implicit operator AssertionData<T>((T?, Exception?, string?) tuple) =>
        new(tuple.Item1, tuple.Item2, tuple.Item3);
}