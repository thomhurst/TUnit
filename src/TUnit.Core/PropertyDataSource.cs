namespace TUnit.Core;

/// <summary>
/// Property data source that provides a single value for property injection
/// </summary>
public sealed class PropertyDataSource
{
    public required string PropertyName { get; init; }
    public required Type PropertyType { get; init; }
    public required IDataSourceAttribute DataSource { get; init; }
}