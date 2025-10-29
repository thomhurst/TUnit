using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test metadata and identity management
/// Implements <see cref="ITestMetadata"/> interface
/// </summary>
public partial class TestContext
{
    // Explicit interface implementations for ITestMetadata
    Guid ITestMetadata.Id => Id;
    TestDetails ITestMetadata.TestDetails => TestDetails;
    string ITestMetadata.TestName => TestDetails.TestName;

    string ITestMetadata.DisplayName
    {
        get => GetDisplayName();
        set => CustomDisplayName = value;
    }

    Type? ITestMetadata.DisplayNameFormatter
    {
        get => DisplayNameFormatter;
        set => DisplayNameFormatter = value;
    }
}
