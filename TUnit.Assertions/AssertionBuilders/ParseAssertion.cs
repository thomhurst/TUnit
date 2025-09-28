using System;
using System.Globalization;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Parse assertion for string values
/// </summary>
public class ParseAssertion<T> : AssertionBase<string?>
#if NET7_0_OR_GREATER
    where T : IParsable<T>
#endif
{
    private readonly bool _shouldBeParsable;
    private IFormatProvider? _formatProvider;

    public ParseAssertion(Func<Task<string?>> actualValueProvider, bool shouldBeParsable)
        : base(actualValueProvider)
    {
        _shouldBeParsable = shouldBeParsable;
    }

    public ParseAssertion(Func<string?> actualValueProvider, bool shouldBeParsable)
        : base(actualValueProvider)
    {
        _shouldBeParsable = shouldBeParsable;
    }

    public ParseAssertion(string? actualValue, bool shouldBeParsable)
        : base(actualValue)
    {
        _shouldBeParsable = shouldBeParsable;
    }

    public ParseAssertion<T> WithFormatProvider(IFormatProvider formatProvider)
    {
        _formatProvider = formatProvider;
        return this;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return _shouldBeParsable
                ? AssertionResult.Fail($"Cannot parse null string to {typeof(T).Name}")
                : AssertionResult.Passed;
        }

#if NET7_0_OR_GREATER
        var provider = _formatProvider ?? CultureInfo.InvariantCulture;
        bool isParsable = T.TryParse(actual, provider, out _);
#else
        // For older frameworks, use reflection to find TryParse method
        bool isParsable = false;
        var type = typeof(T);

        if (_formatProvider != null)
        {
            // Try to find TryParse with IFormatProvider
            var tryParseWithProvider = type.GetMethod("TryParse",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(IFormatProvider), type.MakeByRefType() },
                null);

            if (tryParseWithProvider != null)
            {
                var parameters = new object?[] { actual, _formatProvider, null };
                isParsable = (bool)tryParseWithProvider.Invoke(null, parameters)!;
            }
        }

        if (!isParsable && _formatProvider == null)
        {
            // Try basic TryParse without IFormatProvider
            var tryParseMethod = type.GetMethod("TryParse",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(string), type.MakeByRefType() },
                null);

            if (tryParseMethod != null)
            {
                var parameters = new object?[] { actual, null };
                isParsable = (bool)tryParseMethod.Invoke(null, parameters)!;
            }
        }
#endif

        if (_shouldBeParsable && isParsable)
        {
            return AssertionResult.Passed;
        }
        else if (!_shouldBeParsable && !isParsable)
        {
            return AssertionResult.Passed;
        }
        else if (_shouldBeParsable)
        {
            return AssertionResult.Fail($"Expected '{actual}' to be parsable as {typeof(T).Name}");
        }
        else
        {
            return AssertionResult.Fail($"Expected '{actual}' not to be parsable as {typeof(T).Name}");
        }
    }
}

#if NET7_0_OR_GREATER
/// <summary>
/// Parse assertion that returns the parsed value for further chaining
/// </summary>
public class WhenParsedAssertion<T> : AssertionBuilder<T>
    where T : IParsable<T>
{
    public WhenParsedAssertion(Func<Task<string?>> stringValueProvider, IFormatProvider? formatProvider = null)
        : base(async () =>
        {
            var str = await stringValueProvider();
            if (str == null)
                throw new InvalidOperationException($"Cannot parse null string to {typeof(T).Name}");

            var provider = formatProvider ?? CultureInfo.InvariantCulture;
            if (!T.TryParse(str, provider, out var result))
                throw new InvalidOperationException($"Failed to parse '{str}' as {typeof(T).Name}");

            return result;
        })
    {
    }
}
#else
// Version for older frameworks that don't have IParsable<T>
public class WhenParsedAssertion<T> : AssertionBuilder<T>
{
    public WhenParsedAssertion(Func<Task<string?>> stringValueProvider, IFormatProvider? formatProvider = null)
        : base(async () =>
        {
            var str = await stringValueProvider();
            if (str == null)
                throw new InvalidOperationException($"Cannot parse null string to {typeof(T).Name}");

            // Use reflection to find TryParse method
            var type = typeof(T);
            var tryParseMethod = type.GetMethod("TryParse",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(string), type.MakeByRefType() },
                null);

            if (tryParseMethod == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} does not have a TryParse method");

            var parameters = new object?[] { str, null };
            var success = (bool)tryParseMethod.Invoke(null, parameters)!;

            if (!success)
                throw new InvalidOperationException($"Failed to parse '{str}' as {typeof(T).Name}");

            return (T)parameters[1]!;
        })
    {
    }
}
#endif