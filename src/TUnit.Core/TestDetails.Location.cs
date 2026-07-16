using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test source location - implements <see cref="ITestLocation"/> interface
/// </summary>
public partial class TestDetails
{
    // Explicit interface implementation for ITestLocation
    string ITestLocation.TestFilePath => TestFilePath;
    int ITestLocation.TestLineNumber => TestLineNumber;
}
