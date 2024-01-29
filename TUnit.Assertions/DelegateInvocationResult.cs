namespace TUnit.Assertions;

public record DelegateInvocationResult<T>(T? Result, Exception? Exception)
{
    public static implicit operator DelegateInvocationResult<T>((T?, Exception?) tuple) =>
        new(tuple.Item1, tuple.Item2);
}