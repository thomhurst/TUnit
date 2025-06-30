using TUnit.Core.Models;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Resolves data sources at compile-time for AOT-safe test execution.
/// This interface enables source generators to evaluate data attributes during build.
/// </summary>
public interface ICompileTimeDataResolver
{
    /// <summary>
    /// Resolves class-level data sources at compile-time.
    /// </summary>
    /// <param name="classMetadata">The class metadata containing data attributes</param>
    /// <returns>All resolved data combinations for the class</returns>
    Task<IReadOnlyList<object?[]>> ResolveClassDataAsync(ClassMetadata classMetadata);

    /// <summary>
    /// Resolves method-level data sources at compile-time.
    /// </summary>
    /// <param name="methodMetadata">The method metadata containing data attributes</param>
    /// <returns>All resolved data combinations for the method</returns>
    Task<IReadOnlyList<object?[]>> ResolveMethodDataAsync(MethodMetadata methodMetadata);

    /// <summary>
    /// Resolves property injection data at compile-time.
    /// </summary>
    /// <param name="classMetadata">The class metadata containing property sources</param>
    /// <returns>Resolved property values</returns>
    Task<IDictionary<string, object?>> ResolvePropertyDataAsync(ClassMetadata classMetadata);

    /// <summary>
    /// Checks if a data attribute can be resolved at compile-time.
    /// </summary>
    /// <param name="dataAttribute">The data attribute to check</param>
    /// <returns>True if resolvable at compile-time, false otherwise</returns>
    bool CanResolveAtCompileTime(IDataAttribute dataAttribute);
}

/// <summary>
/// Resolved data for source generation.
/// Contains all data that was successfully resolved at compile-time.
/// </summary>
public sealed class CompileTimeResolvedData
{
    /// <summary>
    /// Class-level data combinations resolved at compile-time.
    /// </summary>
    public IReadOnlyList<object?[]> ClassData { get; init; } = [
    ];

    /// <summary>
    /// Method-level data combinations resolved at compile-time.
    /// </summary>
    public IReadOnlyList<object?[]> MethodData { get; init; } = [
    ];

    /// <summary>
    /// Property values resolved at compile-time.
    /// </summary>
    public IDictionary<string, object?> PropertyData { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Data attributes that could not be resolved at compile-time.
    /// These will need to be handled at runtime in reflection mode.
    /// </summary>
    public IReadOnlyList<IDataAttribute> UnresolvedAttributes { get; init; } = [
    ];

    /// <summary>
    /// Total number of test variations this data will produce.
    /// </summary>
    public int TotalVariations => Math.Max(1, ClassData.Count) * Math.Max(1, MethodData.Count);
}
