namespace TUnit.Core;

/// <summary>
/// A placeholder instance used during test discovery for tests that will have their instances created lazily during execution.
/// This is different from SkippedTestInstance which is for tests that are actually skipped.
/// </summary>
internal sealed class PlaceholderInstance
{
    public static readonly PlaceholderInstance Instance = new();
    
    private PlaceholderInstance()
    {
        // Private constructor to ensure singleton pattern
    }
}