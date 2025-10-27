namespace TUnit.Core.PropertyTesting;

/// <summary>
/// Extension methods for property-based testing support
/// </summary>
public static class PropertyTestExtensions
{
    private const string PropertyTestMetadataKey = "PropertyTestMetadata";

    /// <summary>
    /// Get property test metadata from the test context
    /// </summary>
    /// <param name="context">The test context</param>
    /// <returns>The property test metadata, or null if not a property test</returns>
    public static PropertyTestMetadata? GetPropertyTestMetadata(this TestContext context)
    {
        return context.ObjectBag.TryGetValue(PropertyTestMetadataKey, out var metadata)
            ? metadata as PropertyTestMetadata
            : null;
    }

    /// <summary>
    /// Set property test metadata in the test context
    /// </summary>
    /// <param name="context">The test context</param>
    /// <param name="metadata">The property test metadata</param>
    public static void SetPropertyTestMetadata(this TestContext context, PropertyTestMetadata metadata)
    {
        context.ObjectBag[PropertyTestMetadataKey] = metadata;
    }

    /// <summary>
    /// Check if the current test is a property-based test (original or shrunk)
    /// </summary>
    /// <param name="context">The test context</param>
    /// <returns>True if this is a property test, false otherwise</returns>
    public static bool IsPropertyTest(this TestContext context)
    {
        return context.GetPropertyTestMetadata() != null;
    }

    /// <summary>
    /// Check if the current test is a shrinking test (vs original generated test)
    /// </summary>
    /// <param name="context">The test context</param>
    /// <returns>True if this is a shrinking test, false otherwise</returns>
    public static bool IsShrinkingTest(this TestContext context)
    {
        var metadata = context.GetPropertyTestMetadata();
        return metadata?.IsShrinkingTest ?? false;
    }

    /// <summary>
    /// Get the original test ID for shrinking tests
    /// </summary>
    /// <param name="context">The test context</param>
    /// <returns>The original test ID, or the current test ID if not a property test</returns>
    public static Guid GetOriginalTestId(this TestContext context)
    {
        var metadata = context.GetPropertyTestMetadata();
        return metadata?.OriginalTestId ?? context.Id;
    }

    /// <summary>
    /// Get the current shrink attempt number
    /// </summary>
    /// <param name="context">The test context</param>
    /// <returns>The shrink attempt number (0 for original test)</returns>
    public static int GetShrinkAttempt(this TestContext context)
    {
        var metadata = context.GetPropertyTestMetadata();
        return metadata?.ShrinkAttempt ?? 0;
    }

    /// <summary>
    /// Get the random seed used for test generation
    /// </summary>
    /// <param name="context">The test context</param>
    /// <returns>The random seed, or 0 if not a property test</returns>
    public static long GetRandomSeed(this TestContext context)
    {
        var metadata = context.GetPropertyTestMetadata();
        return metadata?.RandomSeed ?? 0;
    }
}
