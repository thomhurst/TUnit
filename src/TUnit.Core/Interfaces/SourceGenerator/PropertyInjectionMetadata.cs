using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Interfaces.SourceGenerator;

/// <summary>
/// Metadata about a property that needs data source injection, generated at compile-time.
/// </summary>
public sealed class PropertyInjectionMetadata
{
    // Sentinel indicating the property is not readable — distinct from "not yet computed"
    // (null) so the memoized negative result does not keep re-running reflection.
    private static readonly Func<object, object?> s_notReadableSentinel = static _ => null;

    // Source-gen-supplied delegate (or null for hand-authored/reflection-only metadata).
    // Kept separate from the lazy-init reflection fallback so the public GetProperty
    // contract ("what the source generator emitted") is not polluted by internal caching.
    private readonly Func<object, object?>? _sourceGenGetter;

    // Lazily populated reflection-based fallback. volatile so a racing reader observes
    // the completed delegate write without tearing on weakly-ordered architectures;
    // Interlocked.CompareExchange makes the winning writer visible to everyone.
    private volatile Func<object, object?>? _cachedGetter;

    /// <summary>
    /// Gets the name of the property that needs injection.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicProperties)]
    public required Type PropertyType { get; init; }

    /// <summary>
    /// Gets the type that contains the property (the parent class).
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    public required Type ContainingType { get; init; }

    /// <summary>
    /// Gets a factory function that creates the data source attribute instance.
    /// </summary>
    public required Func<IDataSourceAttribute> CreateDataSource { get; init; }

    /// <summary>
    /// Gets a delegate that sets the property value on the target instance.
    /// Handles type casting and property setting (including init-only properties).
    /// </summary>
    public required Action<object, object?> SetProperty { get; init; }

    /// <summary>
    /// Optional compile-time-generated getter that returns the property value from an instance.
    /// When not supplied (e.g. older source-gen output or hand-authored metadata) the callers
    /// fall back to a cached <see cref="PropertyInfo.GetValue(object)"/> call. Supplying a
    /// direct delegate removes a <see cref="Type.GetProperty(string)"/> reflection lookup from
    /// the per-test hot path in both source-gen and reflection modes.
    /// </summary>
    public Func<object, object?>? GetProperty
    {
        get => _sourceGenGetter;
        init => _sourceGenGetter = value;
    }

    /// <summary>
    /// Returns a delegate that reads the property value from a given instance, or <c>null</c>
    /// when the property is not readable (callers should skip such properties). Uses the
    /// source-generated <see cref="GetProperty"/> delegate when available, otherwise compiles
    /// a reflection-based getter once and caches it on the metadata instance.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "ContainingType is annotated to preserve properties; reflection fallback is only used when source-gen did not emit a delegate.")]
    internal Func<object, object?>? GetOrCreateGetter()
    {
        // Fast path: source-gen supplied a delegate.
        if (_sourceGenGetter != null)
        {
            return _sourceGenGetter;
        }

        // Fast path: reflection fallback already cached (volatile read).
        var cached = _cachedGetter;
        if (cached != null)
        {
            return ReferenceEquals(cached, s_notReadableSentinel) ? null : cached;
        }

        var property = ContainingType.GetProperty(PropertyName);
        var computed = property is null || !property.CanRead
            ? s_notReadableSentinel
            : (Func<object, object?>)(obj => property.GetValue(obj));

        // Benign race: both racers compute equivalent delegates; CompareExchange makes the
        // winner visible to any subsequent reader without a lock.
        Interlocked.CompareExchange(ref _cachedGetter, computed, null);
        var winner = _cachedGetter!;
        return ReferenceEquals(winner, s_notReadableSentinel) ? null : winner;
    }
}
