namespace TUnit.Core;

/// <summary>
/// Interface for dynamic test metadata that should bypass normal data source processing
/// </summary>
public interface IDynamicTestMetadata
{
    /// <summary>
    /// Unique index for this dynamic test within its builder context.
    /// Used to generate unique test IDs when multiple dynamic tests target the same method.
    /// </summary>
    int DynamicTestIndex { get; }
}