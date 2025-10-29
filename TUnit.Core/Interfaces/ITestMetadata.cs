namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test metadata and identity.
/// Accessed via <see cref="TestContext.Metadata"/>.
/// </summary>
public interface ITestMetadata
{
    /// <summary>
    /// Gets the unique identifier for this test instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the detailed metadata about this test, including class type, method info, and arguments.
    /// </summary>
    TestDetails TestDetails { get; }

    /// <summary>
    /// Gets the base name of the test method.
    /// </summary>
    string TestName { get; }

    /// <summary>
    /// Gets or sets the display name for the test.
    /// When reading, returns the custom display name if set, otherwise computes from test name and arguments.
    /// Setting this value overrides the default generated name.
    /// </summary>
    string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the custom display name formatter type used to format test names.
    /// Must implement IDisplayNameFormatter interface.
    /// </summary>
    Type? DisplayNameFormatter { get; set; }
}
