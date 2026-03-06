namespace TUnit.Core;

/// <summary>
/// Information about a test variant that was created at runtime.
/// Returned by <see cref="Extensions.TestContextExtensions.CreateTestVariant"/>.
/// </summary>
public sealed record TestVariantInfo
{
    internal TestVariantInfo(string testId, string displayName)
    {
        TestId = testId;
        DisplayName = displayName;
    }

    /// <summary>
    /// The unique identifier assigned to this test variant.
    /// </summary>
    public string TestId { get; }

    /// <summary>
    /// The display name of this test variant as it appears in test explorers.
    /// </summary>
    public string DisplayName { get; }
}
