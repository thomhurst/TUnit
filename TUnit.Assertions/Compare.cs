using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions;

public static class Compare
{
    [RequiresUnreferencedCode("Uses reflection to iterate through nested objects")]
    public static IEnumerable<ComparisonFailure> CheckEquivalent<T>(T actual, T expected, CompareOptions options)
    {
        return CheckEquivalent(actual, expected, options, [], MemberType.Value);
    }
    
    [RequiresUnreferencedCode("Uses reflection to iterate through nested objects")]
    public static IEnumerable<ComparisonFailure> CheckEquivalent<T>(T actual, T expected, CompareOptions options, string[] memberNames, MemberType memberType)
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

                foreach (var comparisonFailure in CheckEquivalent(actualObject, expectedObject, options, [..memberNames, $"[{i}]"], MemberType.EnumerableItem))
                {
                    yield return comparisonFailure;
                }
            }
        }
        
        if (actual.GetType().IsPrimitive 
            || actual.GetType().IsEnum 
            || actual.GetType().IsValueType 
            || actual is string)
        {
            if (!actual.Equals(expected))
            {
                yield return new ComparisonFailure
                {
                    Type = MemberType.Value,
                    Actual = actual,
                    Expected = expected,
                    NestedMemberNames = memberNames
                };
            }
            
            yield break;
        }

        foreach (var fieldInfo in actual.GetType().GetFields().Concat(expected.GetType().GetFields()).Distinct())
        {
            string?[] readOnlySpan = [..memberNames, fieldInfo.Name];
            
            if (options.MembersToIgnore.Contains(string.Join('.', readOnlySpan)))
            {
                continue;
            }
            
            var actualFieldValue = fieldInfo.GetValue(actual);
            var expectedFieldValue = fieldInfo.GetValue(expected);

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

            foreach (var comparisonFailure in CheckEquivalent(actualFieldValue, expectedFieldValue, options, [..memberNames, fieldInfo.Name], MemberType.Field))
            {
                yield return comparisonFailure;
            }
        }
        
        foreach (var propertyInfo in actual.GetType().GetProperties().Concat(expected!.GetType().GetProperties())
                     .Distinct()
                     .Where(p => p.GetIndexParameters().Length == 0))
        {
            string?[] readOnlySpan = [..memberNames, propertyInfo.Name];
            
            if (options.MembersToIgnore.Contains(string.Join('.', readOnlySpan)))
            {
                continue;
            }

            var actualPropertyValue = propertyInfo.GetValue(actual);
            var expectedPropertyValue = propertyInfo.GetValue(expected);

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
            
            foreach (var comparisonFailure in CheckEquivalent(actualPropertyValue, expectedPropertyValue, options, [..memberNames, propertyInfo.Name], MemberType.Property))
            {
                yield return comparisonFailure;
            }
        }
    }
}