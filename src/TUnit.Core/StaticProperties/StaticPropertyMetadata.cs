namespace TUnit.Core.StaticProperties;

/// <summary>
/// Metadata for a static property that needs initialization
/// </summary>
public sealed class StaticPropertyMetadata
{
    public required string PropertyName { get; init; }
    public required Type PropertyType { get; init; }
    public required Type DeclaringType { get; init; }
    public required Func<Task<object?>> InitializerAsync { get; init; }
}
