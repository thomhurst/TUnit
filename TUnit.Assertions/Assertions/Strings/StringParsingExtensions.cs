using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Strings.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Strings;

public static class StringParsingExtensions
{
    /// <summary>
    /// Asserts that the string value can be parsed into the specified target type.
    /// </summary>
    /// <typeparam name="TTarget">The target type to parse the string into.</typeparam>
    /// <param name="valueSource">The source containing the string value to test.</param>
    /// <param name="doNotPopulateThisValue">Do not use. This is populated by the compiler to get the expression of the value.</param>
    /// <returns>An assertion builder that can be used to add a format provider.</returns>
    public static ParseAssertion<string?>
        IsParsableInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TTarget>(
        this IValueSource<string> valueSource,
        [CallerArgumentExpression(nameof(valueSource))] string? doNotPopulateThisValue = null)
    {
        IValueSource<string?> nullableSource = valueSource;
        var assertionBuilder = nullableSource.RegisterAssertion(
            new StringIsParsableCondition<TTarget>(),
            [doNotPopulateThisValue]);

        return new ParseAssertion<string?>((AssertionBuilder<string?>)assertionBuilder);
    }

    /// <summary>
    /// Asserts that the string value cannot be parsed into the specified target type.
    /// </summary>
    /// <typeparam name="TTarget">The target type that the string should not be parsable into.</typeparam>
    /// <param name="valueSource">The source containing the string value to test.</param>
    /// <param name="doNotPopulateThisValue">Do not use. This is populated by the compiler to get the expression of the value.</param>
    /// <returns>An assertion builder that can be used to add a format provider.</returns>
    public static ParseAssertion<string?>
        IsNotParsableInto<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TTarget>(
        this IValueSource<string> valueSource,
        [CallerArgumentExpression(nameof(valueSource))] string? doNotPopulateThisValue = null)
    {
        IValueSource<string?> nullableSource = valueSource;
        var assertionBuilder = nullableSource.RegisterAssertion(
            new StringIsNotParsableCondition<TTarget>(),
            [doNotPopulateThisValue]);

        return new ParseAssertion<string?>((AssertionBuilder<string?>)assertionBuilder);
    }

    /// <summary>
    /// Parses the string value into the specified target type and returns an assertion builder
    /// that can be used to perform further assertions on the parsed value.
    /// </summary>
    /// <typeparam name="TTarget">The target type to parse the string into.</typeparam>
    /// <param name="valueSource">The source containing the string value to parse.</param>
    /// <param name="formatProvider">Optional format provider to use during parsing.</param>
    /// <param name="doNotPopulateThisValue">Do not use. This is populated by the compiler to get the expression of the value.</param>
    /// <returns>An assertion builder for the parsed value.</returns>
    public static AssertionBuilder<TTarget> WhenParsedInto<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TTarget>(
        this IValueSource<string> valueSource,
        IFormatProvider? formatProvider = null,
        [CallerArgumentExpression(nameof(valueSource))] string? doNotPopulateThisValue = null)
    {
        IValueSource<string?> nullableSource = valueSource;
        return nullableSource.RegisterConversionAssertion(
            new ParseConversionAssertCondition<TTarget>(formatProvider),
            [doNotPopulateThisValue]);
    }


}