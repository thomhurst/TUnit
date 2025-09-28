using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for string type assertions
/// </summary>
public static class StringAssertionExtensions
{
    public static GenericEqualToAssertion<string> IsEqualTo(this ValueAssertionBuilder<string> builder, string expected, StringComparison comparisonType)
    {
        Func<string?, string?, bool> comparisonFunc = (actual, exp) =>
            string.Equals(actual, exp, comparisonType);
        return new GenericEqualToAssertion<string>(builder.ActualValueProvider, expected).WithComparison(comparisonFunc);
    }

    public static CustomAssertion<string> IsNotEqualTo(this ValueAssertionBuilder<string> builder, string expected, StringComparison comparisonType)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => !string.Equals(actual, expected, comparisonType),
            $"Expected string to not be equal to '{expected}' using {comparisonType} comparison");
    }

    public static StringContainsAssertion Contains(this ValueAssertionBuilder<string> builder, string substring)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringContainsAssertion(nullableProvider, substring);
    }

    public static StringStartsWithAssertion StartsWith(this ValueAssertionBuilder<string> builder, string prefix)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringStartsWithAssertion(nullableProvider, prefix);
    }

    public static StringEndsWithAssertion EndsWith(this ValueAssertionBuilder<string> builder, string suffix)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringEndsWithAssertion(nullableProvider, suffix);
    }

    public static CustomAssertion<string> HasLength(this ValueAssertionBuilder<string> builder, int expectedLength)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => actual?.Length == expectedLength,
            $"Expected string to have length {expectedLength} but was {{ActualValue?.Length}}");
    }

    public static ValueAssertionBuilder<int> HasLength(this ValueAssertionBuilder<string> builder)
    {
        return new ValueAssertionBuilder<int>(async () =>
        {
            var actual = await builder.ActualValueProvider();
            return actual?.Length ?? 0;
        });
    }

    public static NumericAssertion<int> HasCount(this ValueAssertionBuilder<string> builder)
    {
        return new NumericAssertion<int>(async () =>
        {
            var actual = await builder.ActualValueProvider();
            return actual?.Length ?? 0;
        });
    }

    public static CustomAssertion<string> IsEmpty(this ValueAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => string.IsNullOrEmpty(actual),
            "Expected string to be empty but was {ActualValue}");
    }

    public static CustomAssertion<string> IsNotEmpty(this ValueAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => !string.IsNullOrEmpty(actual),
            "Expected string to not be empty");
    }

    public static CustomAssertion<string> DoesNotContain(this ValueAssertionBuilder<string> builder, string substring)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !s.Contains(substring),
            $"Expected string to not contain '{substring}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> IsNullOrEmpty(this ValueAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => string.IsNullOrEmpty(actual),
            "Expected string to be null or empty but was {ActualValue}");
    }

    public static CustomAssertion<string> IsNotNullOrEmpty(this ValueAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => !string.IsNullOrEmpty(actual),
            "Expected string to not be null or empty");
    }

    public static CustomAssertion<string> IsNullOrWhiteSpace(this ValueAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => string.IsNullOrWhiteSpace(actual),
            "Expected string to be null or whitespace but was {ActualValue}");
    }

    public static CustomAssertion<string> IsNotNullOrWhiteSpace(this ValueAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => !string.IsNullOrWhiteSpace(actual),
            "Expected string to not be null or whitespace");
    }

    public static GenericEqualToAssertion<string> IsEqualTo(this DualAssertionBuilder<string> builder, string expected, StringComparison comparisonType)
    {
        Func<string?, string?, bool> comparisonFunc = (actual, exp) =>
            string.Equals(actual, exp, comparisonType);
        return new GenericEqualToAssertion<string>(builder.ActualValueProvider, expected).WithComparison(comparisonFunc);
    }

    public static CustomAssertion<string> IsNotEqualTo(this DualAssertionBuilder<string> builder, string expected, StringComparison comparisonType)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => !string.Equals(actual, expected, comparisonType),
            $"Expected string to not be equal to '{expected}' using {comparisonType} comparison");
    }

    public static StringContainsAssertion Contains(this DualAssertionBuilder<string> builder, string substring)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringContainsAssertion(nullableProvider, substring);
    }

    public static StringStartsWithAssertion StartsWith(this DualAssertionBuilder<string> builder, string prefix)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringStartsWithAssertion(nullableProvider, prefix);
    }

    public static StringEndsWithAssertion EndsWith(this DualAssertionBuilder<string> builder, string suffix)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringEndsWithAssertion(nullableProvider, suffix);
    }

    public static CustomAssertion<string> HasLength(this DualAssertionBuilder<string> builder, int expectedLength)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => actual?.Length == expectedLength,
            $"Expected string to have length {expectedLength} but was {{ActualValue?.Length}}");
    }

    public static ValueAssertionBuilder<int> HasLength(this DualAssertionBuilder<string> builder)
    {
        return new ValueAssertionBuilder<int>(async () =>
        {
            var actual = await builder.ActualValueProvider();
            return actual?.Length ?? 0;
        });
    }

    public static CustomAssertion<string> IsEmpty(this DualAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => string.IsNullOrEmpty(actual),
            "Expected string to be empty but was {ActualValue}");
    }

    public static CustomAssertion<string> IsNotEmpty(this DualAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => !string.IsNullOrEmpty(actual),
            "Expected string to not be empty");
    }

    public static CustomAssertion<string> DoesNotContain(this DualAssertionBuilder<string> builder, string substring)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !s.Contains(substring),
            $"Expected string to not contain '{substring}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> IsNullOrWhitespace(this ValueAssertionBuilder<string> builder)
    {
        return builder.IsNullOrWhiteSpace();
    }

    public static CustomAssertion<string> IsNotNullOrWhitespace(this ValueAssertionBuilder<string> builder)
    {
        return builder.IsNotNullOrWhiteSpace();
    }

    public static CustomAssertion<string> IsNullOrWhitespace(this DualAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => string.IsNullOrWhiteSpace(actual),
            "Expected string to be null or whitespace but was {ActualValue}");
    }

    public static CustomAssertion<string> IsNotNullOrWhitespace(this DualAssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            actual => !string.IsNullOrWhiteSpace(actual),
            "Expected string to not be null or whitespace");
    }

    public static CustomAssertion<string> Matches(this ValueAssertionBuilder<string> builder, string pattern)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s != null && System.Text.RegularExpressions.Regex.IsMatch(s, pattern),
            $"Expected string to match pattern '{pattern}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> Matches(this ValueAssertionBuilder<string> builder, System.Text.RegularExpressions.Regex regex)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s != null && regex.IsMatch(s),
            $"Expected string to match pattern '{regex}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> DoesNotMatch(this ValueAssertionBuilder<string> builder, string pattern)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !System.Text.RegularExpressions.Regex.IsMatch(s, pattern),
            $"Expected string to not match pattern '{pattern}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> DoesNotMatch(this ValueAssertionBuilder<string> builder, System.Text.RegularExpressions.Regex regex)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !regex.IsMatch(s),
            $"Expected string to not match pattern '{regex}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> Matches(this DualAssertionBuilder<string> builder, string pattern)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s != null && System.Text.RegularExpressions.Regex.IsMatch(s, pattern),
            $"Expected string to match pattern '{pattern}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> Matches(this DualAssertionBuilder<string> builder, System.Text.RegularExpressions.Regex regex)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s != null && regex.IsMatch(s),
            $"Expected string to match pattern '{regex}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> DoesNotMatch(this DualAssertionBuilder<string> builder, string pattern)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !System.Text.RegularExpressions.Regex.IsMatch(s, pattern),
            $"Expected string to not match pattern '{pattern}' but was {{ActualValue}}");
    }

    public static CustomAssertion<string> DoesNotMatch(this DualAssertionBuilder<string> builder, System.Text.RegularExpressions.Regex regex)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !regex.IsMatch(s),
            $"Expected string to not match pattern '{regex}' but was {{ActualValue}}");
    }

}