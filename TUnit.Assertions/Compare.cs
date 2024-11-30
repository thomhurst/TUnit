﻿#pragma warning disable IL2072
#pragma warning disable IL2075

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Assertions;

public static class Compare
{
    private static readonly BindingFlags BindingFlags =
        BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.FlattenHierarchy;

    //[RequiresUnreferencedCode("Uses reflection to iterate through nested objects")]
    public static IEnumerable<ComparisonFailure> CheckEquivalent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        TActual,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        TExpected>(
        TActual actual,
        TExpected expected, CompareOptions options)
    {
        return CheckEquivalent(actual, expected, options, [], MemberType.Value);
    }

    //[RequiresUnreferencedCode("Uses reflection to iterate through nested objects")]
    private static IEnumerable<ComparisonFailure> CheckEquivalent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TExpected>(
        TActual actual,
        TExpected expected, CompareOptions options,
        string[] memberNames, MemberType memberType)
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

        if (actual.GetType().IsPrimitive
            || actual.GetType().IsEnum
            || actual.GetType().IsValueType
            || actual is string)
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

        if (actual is IEnumerable actualEnumerable && expected is IEnumerable expectedEnumerable)
        {
            var actualObjects = actualEnumerable.Cast<object>().ToArray();
            var expectedObjects = expectedEnumerable.Cast<object>().ToArray();

            for (var i = 0; i < Math.Max(actualObjects.Length, expectedObjects.Length); i++)
            {
                string?[] readOnlySpan = [..memberNames, $"[{i}]"];

                if (options.MembersToIgnore.Contains(string.Join('.', readOnlySpan)))
                {
                    continue;
                }

                var actualObject = actualObjects.ElementAtOrDefault(i);
                var expectedObject = expectedObjects.ElementAtOrDefault(i);

                foreach (var comparisonFailure in CheckEquivalent(actualObject, expectedObject, options,
                             [..memberNames, $"[{i}]"], MemberType.EnumerableItem))
                {
                    yield return comparisonFailure;
                }
            }
        }

        foreach (var fieldName in actual.GetType().GetFields().Concat(expected.GetType().GetFields())
                     .Where(x => !x.Name.StartsWith('<'))
                     .Select(x => x.Name)
                     .Distinct())
        {
            string?[] readOnlySpan = [..memberNames, fieldName];

            if (options.MembersToIgnore.Contains(string.Join('.', readOnlySpan)))
            {
                continue;
            }

            var actualFieldValue = actual.GetType().GetField(fieldName, BindingFlags)?.GetValue(actual);
            var expectedFieldValue = expected.GetType().GetField(fieldName, BindingFlags)?.GetValue(expected);

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
                         [..memberNames, fieldName], MemberType.Field))
            {
                yield return comparisonFailure;
            }
        }

        foreach (var propertyName in actual.GetType().GetProperties().Concat(expected.GetType().GetProperties())
                     .Where(p => p.GetIndexParameters().Length == 0)
                     .Select(x => x.Name)
                     .Distinct())
        {
            string?[] readOnlySpan = [..memberNames, propertyName];

            if (options.MembersToIgnore.Contains(string.Join('.', readOnlySpan)))
            {
                continue;
            }

            var actualPropertyValue = actual.GetType().GetProperty(propertyName, BindingFlags)?.GetValue(actual);
            var expectedPropertyValue = expected.GetType().GetProperty(propertyName, BindingFlags)?.GetValue(expected);

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
                         [..memberNames, propertyName], MemberType.Property))
            {
                yield return comparisonFailure;
            }
        }
    }
}