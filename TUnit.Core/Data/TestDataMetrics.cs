namespace TUnit.Core.Data;

/// <summary>
/// Provides metrics and diagnostic information about the test data container state.
/// </summary>
internal class TestDataMetrics
{
    /// <summary>
    /// Gets or sets the number of globally scoped instances.
    /// </summary>
    public int GlobalInstances { get; set; }

    /// <summary>
    /// Gets or sets the number of class-scoped instances.
    /// </summary>
    public int ClassScopedInstances { get; set; }

    /// <summary>
    /// Gets or sets the number of assembly-scoped instances.
    /// </summary>
    public int AssemblyScopedInstances { get; set; }

    /// <summary>
    /// Gets or sets the number of key-scoped instances.
    /// </summary>
    public int KeyScopedInstances { get; set; }

    /// <summary>
    /// Gets or sets the number of tracked nested dependencies.
    /// </summary>
    public int NestedDependencies { get; set; }

    /// <summary>
    /// Gets or sets detailed diagnostic information.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Gets the total number of managed instances across all scopes.
    /// </summary>
    public int TotalInstances => GlobalInstances + ClassScopedInstances + AssemblyScopedInstances + KeyScopedInstances;

    /// <summary>
    /// Returns a string representation of the metrics.
    /// </summary>
    /// <returns>A formatted string containing the metrics.</returns>
    public override string ToString()
    {
        return $"TestDataMetrics: Total={TotalInstances}, Global={GlobalInstances}, " +
               $"Class={ClassScopedInstances}, Assembly={AssemblyScopedInstances}, " +
               $"Key={KeyScopedInstances}, Nested={NestedDependencies}";
    }
}
