namespace TUnit.Core;

/// <summary>
/// Specifies how class data source instances are shared across tests.
/// Used with <see cref="ClassDataSourceAttribute"/> and <see cref="ClassDataSourceAttribute{T}"/>.
/// </summary>
public enum SharedType
{
    /// <summary>
    /// A new instance is created for each test (no sharing). This is the default.
    /// </summary>
    None,

    /// <summary>
    /// The instance is shared across all tests within the same test class.
    /// </summary>
    PerClass,

    /// <summary>
    /// The instance is shared across all tests within the same assembly.
    /// </summary>
    PerAssembly,

    /// <summary>
    /// The instance is shared across all tests in the entire test session.
    /// </summary>
    PerTestSession,

    /// <summary>
    /// The instance is shared across tests that specify the same key.
    /// Use the <c>Key</c> or <c>Keys</c> property on the data source attribute to specify the key.
    /// </summary>
    Keyed,
}
