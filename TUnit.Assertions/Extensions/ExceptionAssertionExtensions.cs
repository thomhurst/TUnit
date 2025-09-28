using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for exception assertions
/// </summary>
public static class ExceptionAssertionExtensions
{
    public static ExceptionAssertion<TException> HasMessageStartingWith<TException>(
        this ExceptionAssertion<TException> assertion, string prefix)
        where TException : Exception
    {
        return assertion.Matching(ex => ex.Message?.StartsWith(prefix) ?? false);
    }

    public static ExceptionAssertion<TException> HasMessageEndingWith<TException>(
        this ExceptionAssertion<TException> assertion, string suffix)
        where TException : Exception
    {
        return assertion.Matching(ex => ex.Message?.EndsWith(suffix) ?? false);
    }

    public static ExceptionAssertion<TException> HasMessageMatching<TException>(
        this ExceptionAssertion<TException> assertion, string pattern)
        where TException : Exception
    {
        return assertion.WithMessageMatching(pattern);
    }

    public static CustomAssertion<TException> HasMessageEqualTo<TException>(
        this ValueAssertionBuilder<TException> builder, string expectedMessage)
        where TException : Exception
    {
        return new CustomAssertion<TException>(builder.ActualValueProvider,
            ex => ex?.Message == expectedMessage,
            $"Expected exception message to be '{expectedMessage}' but was '{{ActualValue?.Message}}'");
    }

    public static ExceptionAssertion<TException> HasMessageStartingWith<TException>(
        this ValueAssertionBuilder<TException> builder, string prefix)
        where TException : Exception
    {
        var exceptionAssertion = new ExceptionAssertion<TException>(
            async () => await builder.ActualValueProvider());
        return exceptionAssertion.Matching(ex => ex.Message?.StartsWith(prefix) ?? false);
    }

    public static ExceptionAssertion<TException> HasMessageEndingWith<TException>(
        this ValueAssertionBuilder<TException> builder, string suffix)
        where TException : Exception
    {
        var exceptionAssertion = new ExceptionAssertion<TException>(
            async () => await builder.ActualValueProvider());
        return exceptionAssertion.Matching(ex => ex.Message?.EndsWith(suffix) ?? false);
    }

    public static ExceptionAssertion<TException> HasMessageContaining<TException>(
        this ValueAssertionBuilder<TException> builder, string substring)
        where TException : Exception
    {
        var exceptionAssertion = new ExceptionAssertion<TException>(
            async () => await builder.ActualValueProvider());
        return exceptionAssertion.HasMessageContaining(substring);
    }

    public static ExceptionAssertion<TException> HasMessageStartingWith<TException>(
        this DualAssertionBuilder<TException> builder, string prefix)
        where TException : Exception
    {
        var exceptionAssertion = new ExceptionAssertion<TException>(
            async () => await builder.ActualValueProvider());
        return exceptionAssertion.Matching(ex => ex.Message?.StartsWith(prefix) ?? false);
    }

    public static ExceptionAssertion<TException> HasMessageEndingWith<TException>(
        this DualAssertionBuilder<TException> builder, string suffix)
        where TException : Exception
    {
        var exceptionAssertion = new ExceptionAssertion<TException>(
            async () => await builder.ActualValueProvider());
        return exceptionAssertion.Matching(ex => ex.Message?.EndsWith(suffix) ?? false);
    }

    public static ExceptionAssertion<TException> HasMessageContaining<TException>(
        this DualAssertionBuilder<TException> builder, string substring)
        where TException : Exception
    {
        var exceptionAssertion = new ExceptionAssertion<TException>(
            async () => await builder.ActualValueProvider());
        return exceptionAssertion.HasMessageContaining(substring);
    }
}