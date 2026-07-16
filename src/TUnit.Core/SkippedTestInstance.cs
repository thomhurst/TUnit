namespace TUnit.Core;

/// <summary>
/// A placeholder instance used for tests that are skipped at discovery time to avoid calling constructors.
/// </summary>
internal sealed class SkippedTestInstance
{
    public static readonly SkippedTestInstance Instance = new();
    
    private SkippedTestInstance()
    {
        // Private constructor to ensure singleton pattern
    }
}