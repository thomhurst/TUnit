namespace TUnit.Core.Services;

/// <summary>
/// Singleton wrapper for ContextBuilder to ensure the same instance is used across the entire application
/// </summary>
public static class ContextBuilderSingleton
{
    private static readonly Lazy<IContextBuilder> _instance = new(() => new ContextBuilder());

    /// <summary>
    /// Gets the singleton instance of the ContextBuilder
    /// </summary>
    public static IContextBuilder Instance => _instance.Value;
    
    /// <summary>
    /// Resets the singleton instance (useful for testing)
    /// </summary>
    internal static void Reset()
    {
        if (_instance.IsValueCreated)
        {
            _instance.Value.Clear();
        }
    }
}