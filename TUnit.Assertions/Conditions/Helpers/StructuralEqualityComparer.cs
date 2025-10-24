using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// An equality comparer that performs structural equivalency comparison for complex objects.
/// For primitive types, strings, dates, enums, and IEquatable types, uses standard equality.
/// For complex objects, performs deep comparison of properties and fields.
/// </summary>
/// <typeparam name="T">The type of objects to compare</typeparam>
[RequiresDynamicCode("Structural equality comparison uses reflection to access object members and is not compatible with AOT")]
public sealed class StructuralEqualityComparer<T> : IEqualityComparer<T>
{
    /// <summary>
    /// Singleton instance of the structural equality comparer.
    /// </summary>
    public static readonly StructuralEqualityComparer<T> Instance = new();

    private StructuralEqualityComparer()
    {
    }

    public bool Equals(T? x, T? y)
    {
        if (x == null && y == null)
        {
            return true;
        }

        if (x == null || y == null)
        {
            return false;
        }

        var type = typeof(T);

        if (IsPrimitiveType(type))
        {
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        return CompareStructurally(x, y, new HashSet<object>(new ReferenceEqualityComparer()));
    }

    public int GetHashCode(T obj)
    {
        if (obj == null)
        {
            return 0;
        }

        return EqualityComparer<T>.Default.GetHashCode(obj);
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Guid)
#if NET6_0_OR_GREATER
               || type == typeof(DateOnly)
               || type == typeof(TimeOnly)
#endif
            ;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "GetType() is acceptable for runtime structural comparison")]
    private bool CompareStructurally(object? x, object? y, HashSet<object> visited)
    {
        if (x == null && y == null)
        {
            return true;
        }

        if (x == null || y == null)
        {
            return false;
        }

        var xType = x.GetType();
        var yType = y.GetType();

        if (IsPrimitiveType(xType))
        {
            return Equals(x, y);
        }

        if (visited.Contains(x))
        {
            return true;
        }

        visited.Add(x);

        if (x is IEnumerable xEnumerable && y is IEnumerable yEnumerable
            && x is not string && y is not string)
        {
            var xList = xEnumerable.Cast<object?>().ToList();
            var yList = yEnumerable.Cast<object?>().ToList();

            if (xList.Count != yList.Count)
            {
                return false;
            }

            for (int i = 0; i < xList.Count; i++)
            {
                if (!CompareStructurally(xList[i], yList[i], visited))
                {
                    return false;
                }
            }

            return true;
        }

        var members = GetMembersToCompare(xType);

        foreach (var member in members)
        {
            var xValue = GetMemberValue(x, member);
            var yValue = GetMemberValue(y, member);

            if (!CompareStructurally(xValue, yValue, visited))
            {
                return false;
            }
        }

        return true;
    }

    private static List<MemberInfo> GetMembersToCompare([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
    {
        var members = new List<MemberInfo>();
        members.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
        return members;
    }

    private static object? GetMemberValue(object obj, MemberInfo member)
    {
        return member switch
        {
            PropertyInfo prop => prop.GetValue(obj),
            FieldInfo field => field.GetValue(obj),
            _ => throw new InvalidOperationException($"Unknown member type: {member.GetType()}")
        };
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
