using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using TUnit.Assertions.Conditions.Helpers;
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

        var result = CompareObjects(
            value,
            _expected,
            "",
            new HashSet<object>(ReferenceEqualityComparer<object>.Instance),
            new HashSet<object>(ReferenceEqualityComparer<object>.Instance));
        return Task.FromResult(result);
    }

    internal AssertionResult CompareObjects(
        object? actual,
        object? expected,
        string path,
        HashSet<object> visitedActual,
        HashSet<object>? visitedExpected = null)
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
        // But don't treat generic value types as primitive if they contain ignored types
        if (TypeHelper.IsPrimitiveOrWellKnownType(actualType) && !ContainsIgnoredGenericArgument(actualType))
        {
            if (!Equals(actual, expected))
            {
                return AssertionResult.Failed($"Property {path} did not match{Environment.NewLine}Expected: {FormatValue(expected)}{Environment.NewLine}Received: {FormatValue(actual)}");
            }
            return AssertionResult.Passed;
        }

        // Handle cycles - check both actual and expected to prevent infinite recursion
        // from cycles in either object graph
        if (visitedActual.Contains(actual))
        {
            return AssertionResult.Passed;
        }

        visitedActual.Add(actual);

        // Also track expected objects to handle cycles in the expected graph
        if (visitedExpected != null)
        {
            if (visitedExpected.Contains(expected))
            {
                return AssertionResult.Passed;
            }

            visitedExpected.Add(expected);
        }

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

                var result = CompareObjects(actualList[i], expectedList[i], itemPath, visitedActual, visitedExpected);
                if (!result.IsPassed)
                {
                    return result;
                }
            }

            return AssertionResult.Passed;
        }

        // Compare properties and fields
        var expectedMembers = ReflectionHelper.GetMembersToCompare(expectedType);

        foreach (var member in expectedMembers)
        {
            var memberPath = string.IsNullOrEmpty(path) ? member.Name : $"{path}.{member.Name}";

            if (_ignoredMembers.Contains(memberPath))
            {
                continue;
            }

            var expectedValue = ReflectionHelper.GetMemberValue(expected, member);

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
                var actualMember = ReflectionHelper.GetMemberInfo(actualType, member.Name);
                if (actualMember == null)
                {
                    continue;
                }

                actualValue = ReflectionHelper.GetMemberValue(actual, actualMember);
            }
            else
            {
                var actualMember = ReflectionHelper.GetMemberInfo(actualType, member.Name);
                if (actualMember == null)
                {
                    return AssertionResult.Failed($"Property {memberPath} did not match{Environment.NewLine}Expected: {FormatValue(expectedValue)}{Environment.NewLine}Received: null");
                }
                actualValue = ReflectionHelper.GetMemberValue(actual, actualMember);
            }

            var result = CompareObjects(actualValue, expectedValue, memberPath, visitedActual, visitedExpected);
            if (!result.IsPassed)
            {
                return result;
            }
        }

        // In non-partial mode, check for extra properties on actual
        if (!_usePartialEquivalency)
        {
            var actualMembers = ReflectionHelper.GetMembersToCompare(actualType);
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
                    var actualValue = ReflectionHelper.GetMemberValue(actual, member);

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

    /// <summary>
    /// Checks if a generic type (like ValueTuple) contains any ignored types as generic arguments.
    /// This prevents tuples containing ignored types from being compared as primitive values.
    /// </summary>
    private bool ContainsIgnoredGenericArgument(Type type)
    {
        if (!type.IsGenericType || _ignoredTypes.Count == 0)
        {
            return false;
        }

        foreach (var genericArg in type.GetGenericArguments())
        {
            if (_ignoredTypes.Contains(genericArg))
            {
                return true;
            }

            // Recursively check nested generic types (e.g., Tuple<Tuple<IgnoreMe, int>, string>)
            if (ContainsIgnoredGenericArgument(genericArg))
            {
                return true;
            }
        }

        return false;
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
        var sourceVariable = ExpressionHelper.ExtractSourceVariable(expressionString);
        var expectedDesc = _expectedExpression ?? "expected value";

        return $"{sourceVariable} to be equivalent to {expectedDesc}";
    }
}
