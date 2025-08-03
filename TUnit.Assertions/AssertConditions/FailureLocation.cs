namespace TUnit.Assertions.AssertConditions;

public record FailureLocation
{
    public long Position { get; }
    public object? ExpectedValue { get; }
    public object? ActualValue { get; }
}
