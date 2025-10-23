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
[RequiresUnreferencedCode("Uses reflection to compare object properties and fields.")]
public class StructuralEquivalencyAssertion<TValue> : Assertion<TValue>
{
    private readonly object? _expected;
    private readonly string? _expectedExpression;
    private bool _usePartialEquivalency;
    private readonly HashSet<string> _ignoredMembers = new();
    private readonly HashSet<Type> _ignoredTypes = new();

    public StructuralEquivalencyAssertion(
        AssertionContext<TValue> context,
        object? expected,
        string? expectedExpression = null)
        : base(context)
    {
        _expected = expected;
        _expectedExpression = expectedExpression;
    }

    /// <summary>
    /// Enables partial equivalency mode where only properties/fields present on the expected object are compared.
    /// </summary>
    public StructuralEquivalencyAssertion<TValue> WithPartialEquivalency()
    {
        _usePartialEquivalency = true;
        Context.ExpressionBuilder.Append(".WithPartialEquivalency()");
        return this;
    }

    /// <summary>
    /// Ignores a specific member path during equivalency comparison.
    /// </summary>
    public StructuralEquivalencyAssertion<TValue> IgnoringMember(string memberPath)
    {
        _ignoredMembers.Add(memberPath);
        Context.ExpressionBuilder.Append($".IgnoringMember(\"{memberPath}\")");
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
        Context.ExpressionBuilder.Append($".IgnoringType<{type.Name}>()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        var result = CompareObjects(value, _expected, "", new HashSet<object>(new ReferenceEqualityComparer()));
        return Task.FromResult(result);
    }

    internal AssertionResult CompareObjects(object? actual, object? expected, string path, HashSet<object> visited)
    {
        // Check for ignored paths
        if (_ignoredMembers.Contains(path))
        {
            return AssertionResult.Passed;
        }

        // Handle nulls
        if (actual == null && expected == null)
        {
            return AssertionResult.Passed;
        }

        if (actual == null)
        {
            return AssertionResult.Failed($"Property {path} did not match{Environment.NewLine}Expected: {expected}{Environment.NewLine}Received: null");
        }

        if (expected == null)
        {
            return AssertionResult.Failed($"Property {path} did not match{Environment.NewLine}Expected: null{Environment.NewLine}Received: {actual.GetType().Name}");
        }

        var actualType = actual.GetType();
        var expectedType = expected.GetType();

        // Handle primitive types and strings
        if (IsPrimitiveType(actualType))
        {
            if (!Equals(actual, expected))
            {
                return AssertionResult.Failed($"Property {path} did not match{Environment.NewLine}Expected: {FormatValue(expected)}{Environment.NewLine}Received: {FormatValue(actual)}");
            }
            return AssertionResult.Passed;
        }

        // Handle cycles
        if (visited.Contains(actual))
        {
            return AssertionResult.Passed;
        }

        visited.Add(actual);

        // Handle enumerables
        if (actual is IEnumerable actualEnumerable && expected is IEnumerable expectedEnumerable
            && !(actual is string) && !(expected is string))
        {
            var actualList = actualEnumerable.Cast<object?>().ToList();
            var expectedList = expectedEnumerable.Cast<object?>().ToList();

            var maxCount = Math.Max(actualList.Count, expectedList.Count);

            for (int i = 0; i < maxCount; i++)
            {
                var itemPath = $"{path}.[{i}]";

                // Skip if this index is ignored
                if (_ignoredMembers.Contains(itemPath))
                {
                    continue;
                }

                // Check for size mismatch at this index
                if (i >= actualList.Count)
                {
                    return AssertionResult.Failed($"{itemPath} did not match{Environment.NewLine}Expected: {FormatValue(expectedList[i])}{Environment.NewLine}Received: null");
                }

                if (i >= expectedList.Count)
                {
                    return AssertionResult.Failed($"{itemPath} did not match{Environment.NewLine}Expected: null{Environment.NewLine}Received: {FormatValue(actualList[i])}");
                }

                var result = CompareObjects(actualList[i], expectedList[i], itemPath, visited);
                if (!result.IsPassed)
                {
                    return result;
                }
            }

            return AssertionResult.Passed;
        }

        // Compare properties and fields
        var expectedMembers = GetMembersToCompare(expectedType);

        foreach (var member in expectedMembers)
        {
            var memberPath = string.IsNullOrEmpty(path) ? member.Name : $"{path}.{member.Name}";

            if (_ignoredMembers.Contains(memberPath))
            {
                continue;
            }

            var expectedValue = GetMemberValue(expected, member);

            // Check if this member's type should be ignored
            var memberType = member switch
            {
                PropertyInfo prop => prop.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null
            };

            if (memberType != null && ShouldIgnoreType(memberType))
            {
                continue;
            }

            object? actualValue;

            // In partial equivalency mode, skip members that don't exist on actual
            if (_usePartialEquivalency)
            {
                var actualMember = GetMemberInfo(actualType, member.Name);
                if (actualMember == null)
                {
                    continue;
                }

                actualValue = GetMemberValue(actual, actualMember);
            }
            else
            {
                var actualMember = GetMemberInfo(actualType, member.Name);
                if (actualMember == null)
                {
                    return AssertionResult.Failed($"Property {memberPath} did not match{Environment.NewLine}Expected: {FormatValue(expectedValue)}{Environment.NewLine}Received: null");
                }
                actualValue = GetMemberValue(actual, actualMember);
            }

            var result = CompareObjects(actualValue, expectedValue, memberPath, visited);
            if (!result.IsPassed)
            {
                return result;
            }
        }

        // In non-partial mode, check for extra properties on actual
        if (!_usePartialEquivalency)
        {
            var actualMembers = GetMembersToCompare(actualType);
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

                    if (memberType != null && ShouldIgnoreType(memberType))
                    {
                        continue;
                    }

                    var memberPath = string.IsNullOrEmpty(path) ? member.Name : $"{path}.{member.Name}";
                    var actualValue = GetMemberValue(actual, member);

                    // Skip properties with null values - they're equivalent to not having the property
                    if (actualValue == null)
                    {
                        continue;
                    }

                    return AssertionResult.Failed($"Property {memberPath} did not match{Environment.NewLine}Expected: null{Environment.NewLine}Received: {actualValue.GetType().Name}");
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

    private bool ShouldIgnoreType(Type type)
    {
        // Check if the type itself should be ignored
        if (_ignoredTypes.Contains(type))
        {
            return true;
        }

        // Check if the type is a nullable value type and its underlying type should be ignored
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null && _ignoredTypes.Contains(underlyingType))
        {
            return true;
        }

        return false;
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
        {
            return property;
        }

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
        {
            return "null";
        }

        if (value is string s)
        {
            return $"\"{s}\"";
        }

        return value.ToString() ?? "null";
    }

    protected override string GetExpectation()
    {
        // Extract the source variable name from the expression builder
        // Format: "Assert.That(variableName).IsEquivalentTo(...)"
        var expressionString = Context.ExpressionBuilder.ToString();
        var sourceVariable = ExtractSourceVariable(expressionString);
        var expectedDesc = _expectedExpression ?? "expected value";

        return $"{sourceVariable} to be equivalent to {expectedDesc}";
    }

    private static string ExtractSourceVariable(string expression)
    {
        // Extract variable name from "Assert.That(variableName)" or similar
        var thatIndex = expression.IndexOf(".That(");
        if (thatIndex >= 0)
        {
            var startIndex = thatIndex + 6; // Length of ".That("
            var endIndex = expression.IndexOf(')', startIndex);
            if (endIndex > startIndex)
            {
                var variable = expression.Substring(startIndex, endIndex - startIndex);
                // Handle lambda expressions like "async () => ..." by returning "value"
                if (variable.Contains("=>") || variable.StartsWith("()"))
                {
                    return "value";
                }
                return variable;
            }
        }

        return "value";
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
