namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a contract for data source types that need to know the key they were created with
/// when using <see cref="SharedType.Keyed"/> sharing.
/// </summary>
/// <remarks>
/// <para>
/// When a class is used as a <c>[ClassDataSource]</c> with <see cref="SharedType.Keyed"/>,
/// implementing this interface allows the instance to receive its sharing key before
/// <see cref="IAsyncInitializer.InitializeAsync"/> is called.
/// </para>
/// <para>
/// This is useful when a single fixture type needs to behave differently depending on which
/// key it was created for, without requiring separate subclasses per key variant.
/// </para>
/// </remarks>
public interface IKeyedDataSource
{
    /// <summary>
    /// Gets or sets the sharing key that this instance was created with.
    /// </summary>
    /// <remarks>
    /// Set by the TUnit framework after construction but before <see cref="IAsyncInitializer.InitializeAsync"/> is called.
    /// </remarks>
    string Key { get; set; }
}
