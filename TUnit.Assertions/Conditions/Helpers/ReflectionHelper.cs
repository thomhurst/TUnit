using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// Helper methods for reflection-based member access.
/// Consolidates reflection logic to ensure consistent behavior and reduce code duplication.
/// </summary>
internal static class ReflectionHelper
{
    /// <summary>
    /// Gets all public instance properties and fields to compare for structural equivalency.
    /// </summary>
    /// <param name="type">The type to get members from.</param>
    /// <returns>A list of PropertyInfo and FieldInfo members.</returns>
    public static List<MemberInfo> GetMembersToCompare(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        Type type)
    {
        var members = new List<MemberInfo>();
        members.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
        return members;
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
