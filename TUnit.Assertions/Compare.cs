#pragma warning disable IL2072
#pragma warning disable IL2075

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public static class Compare
{
    private static readonly BindingFlags BindingFlags =
        BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.FlattenHierarchy;

    public static IEnumerable<ComparisonFailure> CheckEquivalent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TActual,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TExpected>(TActual actual,
        TExpected expected, CompareOptions options, 
        int? index)
    {
        var initialMemberName = InitialMemberName<TActual>(actual, index);
        
        string[] memberNames = string.IsNullOrEmpty(initialMemberName) ? [] : [initialMemberName];
        
        return CheckEquivalent(actual, expected, options, memberNames, MemberType.Value, []);
    }
    
    private static IEnumerable<ComparisonFailure> CheckEquivalent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TActual,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TExpected>(TActual actual,
        TExpected expected, CompareOptions options,
        string[] memberNames, MemberType memberType, HashSet<object> visited)
    {
        if (actual is null && expected is null)
        {
            yield break;
        }

        if (actual is null || expected is null)
        {
            yield return new ComparisonFailure
            {
                Type = memberType,
                Actual = actual,
                Expected = expected,
                NestedMemberNames = memberNames
            };

            yield break;
        }

        if (actual.Equals(expected))
        {
            yield break;
        }

        if (actual.GetType().IsSimpleType())
        {
            yield return new ComparisonFailure
            {
                Type = memberType,
                Actual = actual,
                Expected = expected,
                NestedMemberNames = memberNames
            };

            yield break;
        }

        if (!visited.Add(actual))
        {
            yield break;
        }
        
        if(actual is IDictionary actualDictionary && expected is IDictionary expectedDictionary)
        {
            var keys = actualDictionary.Keys.Cast<object>()
                .Concat(expectedDictionary.Keys.Cast<object>())
                .Distinct()
                .ToArray();

            foreach (var key in keys)
            {
                if (!actualDictionary.Contains(key))
                {
                    yield return new ComparisonFailure
                    {
                        Type = MemberType.DictionaryItem,
                        Actual = $"No entry with key: {key}",
                        Expected = $"[{key}] = {expectedDictionary[key]}",
                        NestedMemberNames = [..memberNames, $"[{key}]"]
                    };

                    yield break;
                }
                
                if (!expectedDictionary.Contains(key))
                {
                    yield return new ComparisonFailure
                    {
                        Type = MemberType.DictionaryItem,
                        Actual = $"[{key}] = {actualDictionary[key]}",
                        Expected = $"No entry with key: {key}",
                        NestedMemberNames = [..memberNames, $"[{key}]"]
                    };

                    yield break;
                }
                
                var actualObject = actualDictionary[key];
                var expectedObject = expectedDictionary[key];

                foreach (var comparisonFailure in CheckEquivalent(actualObject, expectedObject, options,
                             [..memberNames, $"[{key}]"], MemberType.EnumerableItem, visited))
                {
                    yield return comparisonFailure;
                }
            }
            yield break;
        }

        if (actual is IEnumerable actualEnumerable && expected is IEnumerable expectedEnumerable)
        {
            var actualObjects = actualEnumerable.Cast<object>().ToArray();
            var expectedObjects = expectedEnumerable.Cast<object>().ToArray();

            for (var i = 0; i < Math.Max(actualObjects.Length, expectedObjects.Length); i++)
            {
                string?[] readOnlySpan = [..memberNames.Skip(1), $"[{i}]"];

                if (options.MembersToIgnore.Contains(string.Join(".", readOnlySpan)))
                {
                    continue;
                }

                var actualObject = actualObjects.ElementAtOrDefault(i);
                var expectedObject = expectedObjects.ElementAtOrDefault(i);

                foreach (var comparisonFailure in CheckEquivalent(actualObject, expectedObject, options,
                             [..memberNames, $"[{i}]"], MemberType.EnumerableItem, visited))
                {
                    yield return comparisonFailure;
                }
            }
            
            yield break;
        }

        foreach (var fieldName in actual.GetType().GetFields().Concat(expected.GetType().GetFields())
                     .Where(x => !x.Name.StartsWith('<'))
                     .Select(x => x.Name)
                     .Distinct())
        {
            string?[] readOnlySpan = [..memberNames.Skip(1), fieldName];

            if (options.MembersToIgnore.Contains(string.Join(".", readOnlySpan)))
            {
                continue;
            }

            var actualFieldInfo = actual.GetType().GetField(fieldName, BindingFlags);
            var expectedFieldInfo = expected.GetType().GetField(fieldName, BindingFlags);

            if (options.EquivalencyKind == EquivalencyKind.Partial && expectedFieldInfo is null)
            {
                continue;
            }
            
            var actualFieldValue = actualFieldInfo?.GetValue(actual);
            var expectedFieldValue = expectedFieldInfo?.GetValue(expected);

            if (actualFieldValue?.Equals(actual) == true && expectedFieldValue?.Equals(expected) == true)
            {
                // To prevent cyclical references/stackoverflow
                continue;
            }

            if (actualFieldValue?.Equals(actual) == true || expectedFieldValue?.Equals(expected) == true)
            {
                yield return new ComparisonFailure
                {
                    Type = MemberType.Value,
                    Actual = actual,
                    Expected = expected,
                    NestedMemberNames = memberNames
                };

                yield break;
            }

            foreach (var comparisonFailure in CheckEquivalent(actualFieldValue, expectedFieldValue, options,
                         [..memberNames, fieldName], MemberType.Field, visited))
            {
                yield return comparisonFailure;
            }
        }

        foreach (var propertyName in actual.GetType().GetProperties().Concat(expected.GetType().GetProperties())
                     .Where(p => p.GetIndexParameters().Length == 0)
                     .Select(x => x.Name)
                     .Distinct())
        {
            string?[] readOnlySpan = [..memberNames.Skip(1), propertyName];

            if (options.MembersToIgnore.Contains(string.Join(".", readOnlySpan)))
            {
                continue;
            }

            var actualPropertyInfo = actual.GetType().GetProperty(propertyName, BindingFlags);
            var expectedPropertyInfo = expected.GetType().GetProperty(propertyName, BindingFlags);

            if (options.EquivalencyKind == EquivalencyKind.Partial && expectedPropertyInfo is null)
            {
                continue;
            }
            
            var actualPropertyValue = actualPropertyInfo?.GetValue(actual);
            var expectedPropertyValue = expectedPropertyInfo?.GetValue(expected);

            if (actualPropertyValue?.Equals(actual) == true && expectedPropertyValue?.Equals(expected) == true)
            {
                // To prevent cyclical references/stackoverflow
                continue;
            }

            if (actualPropertyValue?.Equals(actual) == true || actualPropertyValue?.Equals(expected) == true)
            {
                yield return new ComparisonFailure
                {
                    Type = MemberType.Value,
                    Actual = actual,
                    Expected = expected,
                    NestedMemberNames = memberNames
                };

                yield break;
            }

            foreach (var comparisonFailure in CheckEquivalent(actualPropertyValue, expectedPropertyValue, options,
                         [..memberNames, propertyName], MemberType.Property, visited))
            {
                yield return comparisonFailure;
            }
        }
    }

    private static string InitialMemberName<TActual>(object? actual, int? index)
    {
        if (actual is IEnumerable)
        {
            return string.Empty;
        }
        
        var type = actual?.GetType().Name ?? typeof(TActual).Name;
        
        if (index is null)
        {
            return type;
        }
        
        return $"{type}[{index}]";
    }
}