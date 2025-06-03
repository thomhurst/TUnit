using TUnit.Core.Helpers;

namespace TUnit.Core.Data;

/// <summary>
/// Represents an instance with its usage counter for scoped test data management.
/// </summary>
internal class ScopedInstance
{
    /// <summary>
    /// Gets the object instance.
    /// </summary>
    public object Instance { get; }

    /// <summary>
    /// Gets the usage counter for this instance.
    /// </summary>
    public Counter UsageCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedInstance"/> class.
    /// </summary>
    /// <param name="instance">The object instance.</param>
    /// <param name="counter">The usage counter.</param>
    public ScopedInstance(object instance, Counter counter)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        UsageCount = counter ?? throw new ArgumentNullException(nameof(counter));
    }
}
