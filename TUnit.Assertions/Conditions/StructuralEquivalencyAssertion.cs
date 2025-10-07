using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that two objects are structurally equivalent by comparing their properties and fields.
/// Supports partial equivalency and member exclusion.
/// </summary>
public class StructuralEquivalencyAssertion<TValue> : Assertion<TValue>
{
    private readonly object? _expected;
    private bool _usePartialEquivalency;
    private readonly HashSet<string> _ignoredMembers = new();
    private readonly HashSet<Type> _ignoredTypes = new();

    public StructuralEquivalencyAssertion(
        EvaluationContext<TValue> context,
        object? expected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expected = expected;
    }

    /// <summary>
    /// Enables partial equivalency mode where only properties/fields present on the expected object are compared.
    /// </summary>
    public StructuralEquivalencyAssertion<TValue> WithPartialEquivalency()
    {
        _usePartialEquivalency = true;
        ExpressionBuilder.Append(".WithPartialEquivalency()");
        return this;
    }

    /// <summary>
    /// Ignores a specific member path during equivalency comparison.
    /// </summary>
    public StructuralEquivalencyAssertion<TValue> IgnoringMember(string memberPath)
    {
        _ignoredMembers.Add(memberPath);
        ExpressionBuilder.Append($".IgnoringMember(\"{memberPath}\")");
        return this;
    }

    /// <summary>
    /// Ignores all properties/fields of a specific type during equivalency comparison.
    /// </summary>
    public StructuralEquivalencyAssertion<TValue> IgnoringType<T>()
    {
        return IgnoringType(typeof(T));
    }

    /// <summary>
    /// Ignores all properties/fields of a specific type during equivalency comparison.
    /// </summary>
    public StructuralEquivalencyAssertion<TValue> IgnoringType(Type type)
    {
        _ignoredTypes.Add(type);
        ExpressionBuilder.Append($".IgnoringType<{type.Name}>()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));

        var result = CompareObjects(value, _expected, "", new HashSet<object>(new ReferenceEqualityComparer()));
        return Task.FromResult(result);
    }

    internal AssertionResult CompareObjects(object? actual, object? expected, string path, HashSet<object> visited)
    {
        // Check for ignored paths
        if (_ignoredMembers.Contains(path))
            return AssertionResult.Passed;

        // Handle nulls
        if (actual == null && expected == null)
            return AssertionResult.Passed;

        if (actual == null)
            return AssertionResult.Failed($"Property {path} did not match\nExpected: {expected}\nReceived: null");

        if (expected == null)
            return AssertionResult.Failed($"Property {path} did not match\nExpected: null\nReceived: {actual.GetType().Name}");

        var actualType = actual.GetType();
        var expectedType = expected.GetType();

        // Handle primitive types and strings
        if (IsPrimitiveType(actualType))
        {
            if (!Equals(actual, expected))
            {
                return AssertionResult.Failed($"Property {path} did not match\nExpected: {FormatValue(expected)}\nReceived: {FormatValue(actual)}");
            }
            return AssertionResult.Passed;
        }

        // Handle cycles
        if (visited.Contains(actual))
            return AssertionResult.Passed;

        visited.Add(actual);

        // Handle enumerables
        if (actual is IEnumerable actualEnumerable && expected is IEnumerable expectedEnumerable
            && !(actual is string) && !(expected is string))
        {
            var actualList = actualEnumerable.Cast<object?>().ToList();
            var expectedList = expectedEnumerable.Cast<object?>().ToList();

            if (actualList.Count != expectedList.Count)
            {
                return AssertionResult.Failed($"{path}.[{actualList.Count}] did not match\nExpected: null\nReceived: {FormatValue(actualList[actualList.Count - 1])}");
            }

            for (int i = 0; i < actualList.Count; i++)
            {
                var itemPath = $"{path}.[{i}]";
                var result = CompareObjects(actualList[i], expectedList[i], itemPath, visited);
                if (!result.IsPassed)
                    return result;
            }

            return AssertionResult.Passed;
        }

        // Compare properties and fields
#pragma warning disable IL2072 // GetType() does not preserve DynamicallyAccessedMembers - acceptable for runtime structural comparison
        var expectedMembers = GetMembersToCompare(expectedType);
#pragma warning restore IL2072

        foreach (var member in expectedMembers)
        {
            var memberPath = string.IsNullOrEmpty(path) ? member.Name : $"{path}.{member.Name}";

            if (_ignoredMembers.Contains(memberPath))
                continue;

            var expectedValue = GetMemberValue(expected, member);

            // Check if this member's type should be ignored
            var memberType = member switch
            {
                PropertyInfo prop => prop.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null
            };

            if (memberType != null && _ignoredTypes.Contains(memberType))
                continue;
            object? actualValue;

            // In partial equivalency mode, skip members that don't exist on actual
            if (_usePartialEquivalency)
            {
#pragma warning disable IL2072 // GetType() does not preserve DynamicallyAccessedMembers - acceptable for runtime structural comparison
                var actualMember = GetMemberInfo(actualType, member.Name);
#pragma warning restore IL2072
                if (actualMember == null)
                    continue;
                actualValue = GetMemberValue(actual, actualMember);
            }
            else
            {
#pragma warning disable IL2072 // GetType() does not preserve DynamicallyAccessedMembers - acceptable for runtime structural comparison
                var actualMember = GetMemberInfo(actualType, member.Name);
#pragma warning restore IL2072
                if (actualMember == null)
                {
                    return AssertionResult.Failed($"Property {memberPath} did not match\nExpected: {FormatValue(expectedValue)}\nReceived: null");
                }
                actualValue = GetMemberValue(actual, actualMember);
            }

            var result = CompareObjects(actualValue, expectedValue, memberPath, visited);
            if (!result.IsPassed)
                return result;
        }

        // In non-partial mode, check for extra properties on actual
        if (!_usePartialEquivalency)
        {
#pragma warning disable IL2072 // GetType() does not preserve DynamicallyAccessedMembers - acceptable for runtime structural comparison
            var actualMembers = GetMembersToCompare(actualType);
#pragma warning restore IL2072
            var expectedMemberNames = new HashSet<string>(expectedMembers.Select(m => m.Name));

            foreach (var member in actualMembers)
            {
                if (!expectedMemberNames.Contains(member.Name))
                {
                    // Check if this member's type should be ignored
                    var memberType = member switch
                    {
                        PropertyInfo prop => prop.PropertyType,
                        FieldInfo field => field.FieldType,
                        _ => null
                    };

                    if (memberType != null && _ignoredTypes.Contains(memberType))
                        continue;

                    var memberPath = string.IsNullOrEmpty(path) ? member.Name : $"{path}.{member.Name}";
                    var actualValue = GetMemberValue(actual, member);
                    return AssertionResult.Failed($"Property {memberPath} did not match\nExpected: null\nReceived: {actualValue?.GetType().Name ?? "null"}");
                }
            }
        }

        return AssertionResult.Passed;
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
               || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
               || type == typeof(Guid);
    }

    private static List<MemberInfo> GetMembersToCompare([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
    {
        var members = new List<MemberInfo>();
        members.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
        return members;
    }

    private static MemberInfo? GetMemberInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property != null)
            return property;

        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        return field;
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

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is string s)
            return $"\"{s}\"";

        return value.ToString() ?? "null";
    }

    protected override string GetExpectation() => "to be equivalent";

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
