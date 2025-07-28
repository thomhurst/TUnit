using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[DebuggerDisplay("{Type}")]
public record ClassMetadata : MemberMetadata
{
    private static readonly ConcurrentDictionary<string, ClassMetadata> Cache = [];

    public static ClassMetadata GetOrAdd(string name, Func<ClassMetadata> factory)
    {
        // First try to get existing value
        if (Cache.TryGetValue(name, out var existing))
        {
            // If Parameters is empty but we're trying to add one with parameters,
            // update the cache with the new value
            var newValue = factory();
            if (existing.Parameters.Length == 0 && newValue.Parameters.Length > 0)
            {
                Cache.TryUpdate(name, newValue, existing);
                return newValue;
            }
            return existing;
        }
        
        // Otherwise add new value
        return Cache.GetOrAdd(name, _ => factory());
    }

    public virtual bool Equals(ClassMetadata? other)
    {
        return Type == other?.Type;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }

    public required TypeReference TypeReference { get; init; }

    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override required Type Type { get; init; }

    public required string? Namespace { get; init; }
    public required AssemblyMetadata Assembly { get; init; }
    public required ParameterMetadata[] Parameters { get; init; }

    public required PropertyMetadata[] Properties { get; init; }
    public required ClassMetadata? Parent { get; init; }
}
