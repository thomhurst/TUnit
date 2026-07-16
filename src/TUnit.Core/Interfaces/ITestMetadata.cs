namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test metadata and identity.
/// Accessed via <see cref="TestContext.Metadata"/>.
/// </summary>
public interface ITestMetadata
{
    /// <summary>
    /// Gets the unique identifier for the test definition (template/source) that generated this test.
    /// This ID is shared across all instances of parameterized tests.
    /// </summary>
    string DefinitionId { get; }

    /// <summary>
    /// Gets the detailed metadata about this test, including class type, method info, and arguments.
    /// </summary>
    TestDetails TestDetails { get; internal set; }

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
