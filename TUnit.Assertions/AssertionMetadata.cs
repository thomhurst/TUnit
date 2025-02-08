namespace TUnit.Assertions;

public class AssertionMetadata
{
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
}