namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test source code location properties.
/// Accessed via <see cref="TestDetails.Location"/>.
/// </summary>
public interface ITestLocation
{
    /// <summary>
    /// Gets the file path of the source file containing this test.
    /// </summary>
    string TestFilePath { get; }

    /// <summary>
    /// Gets the line number in the source file where this test is defined.
    /// </summary>
    int TestLineNumber { get; }
}
