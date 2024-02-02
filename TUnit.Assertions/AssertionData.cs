namespace TUnit.Assertions;

public record AssertionData<T>(T? Result, Exception? Exception)
{
    public static implicit operator AssertionData<T>((T?, Exception?) tuple) =>
        new(tuple.Item1, tuple.Item2);
}