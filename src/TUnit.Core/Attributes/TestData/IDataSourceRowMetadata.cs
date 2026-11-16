namespace TUnit.Core;

/// <summary>
/// The row settings of TUnit's own data source attributes, read without reflection.
/// </summary>
internal interface IDataSourceRowMetadata
{
    string? DisplayName { get; }

    string? Skip { get; }

    string[]? Categories { get; }
}
