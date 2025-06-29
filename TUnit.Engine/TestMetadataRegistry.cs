namespace TUnit.Engine;

/// <summary>
/// Registry for test metadata sources
/// </summary>
public static class TestMetadataRegistry
{
    private static readonly List<ITestMetadataSource> _sources = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Registers a test metadata source
    /// </summary>
    public static void RegisterSource(ITestMetadataSource source)
    {
        lock (_lock)
        {
            _sources.Add(source);
        }
    }

    /// <summary>
    /// Gets all registered sources
    /// </summary>
    public static ITestMetadataSource[] GetSources()
    {
        lock (_lock)
        {
            return _sources.ToArray();
        }
    }

    /// <summary>
    /// Clears all registered sources (useful for testing)
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _sources.Clear();
        }
    }
}
