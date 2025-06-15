namespace TUnit.Core.Data;

/// <summary>
/// Provides context for data source creation and initialization,
/// allowing the framework to track dependencies without coupling
/// the data container to dependency tracking logic.
/// </summary>
public class DataSourceContext
{
    /// <summary>
    /// Gets or sets the dependency tracker provided by the framework.
    /// This allows both source generation and reflection modes to provide their own tracking.
    /// </summary>
    public IDependencyTracker? DependencyTracker { get; set; }
    
    /// <summary>
    /// Gets or sets the parent object that is being initialized.
    /// Used to establish dependency relationships.
    /// </summary>
    public object? ParentObject { get; set; }
    
    /// <summary>
    /// Gets or sets additional metadata about the data generation context.
    /// </summary>
    public DataGeneratorMetadata? Metadata { get; set; }
    
    /// <summary>
    /// Registers a dependency between the parent object and a child object.
    /// </summary>
    /// <param name="child">The child object that the parent depends on.</param>
    /// <param name="childSharedType">The shared type of the child object.</param>
    /// <param name="childKey">The key of the child object (for keyed sharing).</param>
    public void RegisterDependency(object child, SharedType childSharedType, string? childKey = null)
    {
        if (ParentObject != null && DependencyTracker != null)
        {
            DependencyTracker.RegisterDependency(ParentObject, child, childSharedType, childKey);
        }
    }
}

/// <summary>
/// Interface for dependency tracking that can be implemented by the framework.
/// </summary>
public interface IDependencyTracker
{
    /// <summary>
    /// Registers a dependency between a parent and child object.
    /// </summary>
    void RegisterDependency(object parent, object child, SharedType childSharedType, string? childKey);
    
    /// <summary>
    /// Disposes an object and all its nested dependencies.
    /// </summary>
    Task DisposeWithDependenciesAsync(object item);
}