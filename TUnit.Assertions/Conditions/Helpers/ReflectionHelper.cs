using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// Helper methods for reflection-based member access.
/// Consolidates reflection logic to ensure consistent behavior and reduce code duplication.
/// </summary>
internal static class ReflectionHelper
{
    // Cached per-type member lists. Reflection traversal (especially in StructuralDiffHelper,
    // which calls this once per recursion level and again for closest-match scoring) showed up
    // as a hot path; caching turns the second-and-subsequent calls into a dictionary lookup.
    // Stored as MemberInfo[] so callers can foreach without boxing the enumerator.
    private static readonly ConcurrentDictionary<Type, MemberInfo[]> _membersCache = new();

    /// <summary>
    /// Gets all public instance properties and fields to compare for structural equivalency.
    /// Filters out indexed properties (like indexers) that require parameters.
    /// Result is cached per <see cref="Type"/>. Returned as an array so <c>foreach</c>
    /// uses the language's array iteration pattern with no enumerator allocation.
    /// </summary>
    /// <param name="type">The type to get members from.</param>
    /// <returns>An array of PropertyInfo and FieldInfo members. Do not mutate.</returns>
    public static MemberInfo[] GetMembersToCompare(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        Type type)
        => _membersCache.GetOrAdd(type, BuildMembersToCompare);

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Caller annotates the type with PublicProperties | PublicFields; the cache delegate forwards the same access requirements")]
    private static MemberInfo[] BuildMembersToCompare(Type type)
    {
        var members = new List<MemberInfo>();

        // Filter out indexed properties (properties with parameters like this[int index])
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (prop.GetIndexParameters().Length == 0 && prop.CanRead && prop.GetMethod?.IsPublic == true)
            {
                members.Add(prop);
            }
        }

        members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
        return members.ToArray();
    }

    /// <summary>
    /// Gets the value of a member (property or field) from an object.
    /// </summary>
    /// <param name="obj">The object to get the value from.</param>
    /// <param name="member">The member (PropertyInfo or FieldInfo) to read.</param>
    /// <returns>The value of the member.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the member is not a PropertyInfo or FieldInfo.</exception>
    public static object? GetMemberValue(object obj, MemberInfo member)
    {
        return member switch
        {
            PropertyInfo prop => prop.GetValue(obj),
            FieldInfo field => field.GetValue(obj),
            _ => throw new InvalidOperationException($"Unknown member type: {member.GetType()}")
        };
    }

    /// <summary>
    /// Gets a member (property or field) by name from a type.
    /// </summary>
    /// <param name="type">The type to search.</param>
    /// <param name="name">The member name to find.</param>
    /// <returns>The MemberInfo if found; null otherwise.</returns>
    public static MemberInfo? GetMemberInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        Type type,
        string name)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property != null)
        {
            return property;
        }

        return type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
    }
}
