using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Assertions.Strings;

/// <summary>
/// Asserts that a string can be parsed into the specified type.
/// </summary>
public class IsParsableIntoAssertion<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] T> : Assertion<string>
{
    private IFormatProvider? _formatProvider;

    public IsParsableIntoAssertion(
        AssertionContext<string> context)
        : base(context)
    {
    }

    /// <summary>
    /// Specifies the format provider to use when parsing.
    /// </summary>
    public IsParsableIntoAssertion<T> WithFormatProvider(IFormatProvider formatProvider)
    {
        _formatProvider = formatProvider;
        Context.ExpressionBuilder.Append($".WithFormatProvider(formatProvider)");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (TryParse(value, _formatProvider, out _))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"\"{value}\" cannot be parsed into {typeof(T).Name}"));
    }

    protected override string GetExpectation() => $"to be parsable into {typeof(T).Name}";

    private static bool TryParse(string value, IFormatProvider? formatProvider, out T? result)
    {
#if NET8_0_OR_GREATER
        if (typeof(T).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IParsable<>)))
        {
            var method = typeof(T).GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(IFormatProvider), typeof(T).MakeByRefType() },
                null);

            if (method != null)
            {
                var parameters = new object?[] { value, formatProvider, null };
                var success = (bool)method.Invoke(null, parameters)!;
                result = success ? (T?)parameters[2] : default;
                return success;
            }
        }
#endif

        // Fallback: Use reflection to find TryParse method
        var tryParseMethod = typeof(T).GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(IFormatProvider), typeof(T).MakeByRefType() },
            null);

        if (tryParseMethod != null)
        {
            var parameters = new object?[] { value, formatProvider, null };
            var success = (bool)tryParseMethod.Invoke(null, parameters)!;
            result = success ? (T?)parameters[2] : default;
            return success;
        }

        // Try without format provider
        var tryParseMethodSimple = typeof(T).GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(T).MakeByRefType() },
            null);

        if (tryParseMethodSimple != null)
        {
            var parameters = new object?[] { value, null };
            var success = (bool)tryParseMethodSimple.Invoke(null, parameters)!;
            result = success ? (T?)parameters[1] : default;
            return success;
        }

        result = default;
        return false;
    }
}

/// <summary>
/// Asserts that a string cannot be parsed into the specified type.
/// </summary>
public class IsNotParsableIntoAssertion<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] T> : Assertion<string>
{
    private IFormatProvider? _formatProvider;

    public IsNotParsableIntoAssertion(
        AssertionContext<string> context)
        : base(context)
    {
    }

    /// <summary>
    /// Specifies the format provider to use when parsing.
    /// </summary>
    public IsNotParsableIntoAssertion<T> WithFormatProvider(IFormatProvider formatProvider)
    {
        _formatProvider = formatProvider;
        Context.ExpressionBuilder.Append($".WithFormatProvider(formatProvider)");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Passed); // null cannot be parsed
        }

        if (!TryParse(value, _formatProvider, out _))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"\"{value}\" can be parsed into {typeof(T).Name}"));
    }

    protected override string GetExpectation() => $"to not be parsable into {typeof(T).Name}";

    private static bool TryParse(string value, IFormatProvider? formatProvider, out T? result)
    {
#if NET8_0_OR_GREATER
        if (typeof(T).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IParsable<>)))
        {
            var method = typeof(T).GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(IFormatProvider), typeof(T).MakeByRefType() },
                null);

            if (method != null)
            {
                var parameters = new object?[] { value, formatProvider, null };
                var success = (bool)method.Invoke(null, parameters)!;
                result = success ? (T?)parameters[2] : default;
                return success;
            }
        }
#endif

        // Fallback: Use reflection to find TryParse method
        var tryParseMethod = typeof(T).GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(IFormatProvider), typeof(T).MakeByRefType() },
            null);

        if (tryParseMethod != null)
        {
            var parameters = new object?[] { value, formatProvider, null };
            var success = (bool)tryParseMethod.Invoke(null, parameters)!;
            result = success ? (T?)parameters[2] : default;
            return success;
        }

        // Try without format provider
        var tryParseMethodSimple = typeof(T).GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(T).MakeByRefType() },
            null);

        if (tryParseMethodSimple != null)
        {
            var parameters = new object?[] { value, null };
            var success = (bool)tryParseMethodSimple.Invoke(null, parameters)!;
            result = success ? (T?)parameters[1] : default;
            return success;
        }

        result = default;
        return false;
    }
}

/// <summary>
/// Parses a string into the specified type and returns an assertion on the parsed value.
/// Allows chaining assertions on the parsed result.
/// </summary>
public class WhenParsedIntoAssertion<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] T> : Assertion<T>, IAssertionSource<T>
{
    private readonly IFormatProvider? _formatProvider;

    AssertionContext<T> IAssertionSource<T>.Context => Context;

    public WhenParsedIntoAssertion(
        AssertionContext<string> stringContext,
        IFormatProvider? formatProvider = null)
        : base(new AssertionContext<T>(CreateParsedContext(stringContext.Evaluation, formatProvider), stringContext.ExpressionBuilder))
    {
        // Transfer pending links from string context to handle cross-type chaining
        // e.g., Assert.That(str).HasLength(4).And.WhenParsedInto<int>()
        var (pendingAssertion, combinerType) = stringContext.ConsumePendingLink();
        if (pendingAssertion != null)
        {
            // Store the pending assertion execution as pre-work
            // It will be executed before any assertions on the parsed value
            Context.PendingPreWork = async () => await pendingAssertion.ExecuteCoreAsync();
        }

        _formatProvider = formatProvider;
    }

    /// <summary>
    /// Specifies the format provider to use when parsing.
    /// Returns a new assertion with the format provider set.
    /// </summary>
    public WhenParsedIntoAssertion<T> WithFormatProvider(IFormatProvider formatProvider)
    {
        Context.ExpressionBuilder.Append($".WithFormatProvider(formatProvider)");

        // We need to get the original string context - this is a limitation of the current design
        // For now, return a new instance (this won't work perfectly, but it's the best we can do)
        throw new NotSupportedException(
            "WithFormatProvider must be called before WhenParsedInto. " +
            "Use: Assert.That(str).WhenParsedInto<T>().WithFormatProvider(provider) is not supported. " +
            "The design needs to be reconsidered.");
    }

    private static EvaluationContext<T> CreateParsedContext(
        EvaluationContext<string> stringContext,
        IFormatProvider? formatProvider)
    {
        return new EvaluationContext<T>(async () =>
        {
            var (stringValue, exception) = await stringContext.GetAsync();

            if (exception != null)
            {
                return (default(T)!, exception);
            }

            if (stringValue == null)
            {
                return (default(T)!, new ArgumentNullException(nameof(stringValue), "Cannot parse null string"));
            }

            if (TryParse(stringValue, formatProvider, out var result))
            {
                return (result!, null);
            }

            return (default(T)!, new FormatException($"Cannot parse \"{stringValue}\" into {typeof(T).Name}"));
        });
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<T> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        // WhenParsedInto doesn't perform its own check - it just transforms the value
        // The actual assertion will be done by chained assertions
        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"parsing failed: {exception.Message}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"to be parsable into {typeof(T).Name}";

    private static bool TryParse(string value, IFormatProvider? formatProvider, out T? result)
    {
#if NET8_0_OR_GREATER
        if (typeof(T).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IParsable<>)))
        {
            var method = typeof(T).GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(IFormatProvider), typeof(T).MakeByRefType() },
                null);

            if (method != null)
            {
                var parameters = new object?[] { value, formatProvider, null };
                var success = (bool)method.Invoke(null, parameters)!;
                result = success ? (T?)parameters[2] : default;
                return success;
            }
        }
#endif

        // Fallback: Use reflection to find TryParse method
        var tryParseMethod = typeof(T).GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(IFormatProvider), typeof(T).MakeByRefType() },
            null);

        if (tryParseMethod != null)
        {
            var parameters = new object?[] { value, formatProvider, null };
            var success = (bool)tryParseMethod.Invoke(null, parameters)!;
            result = success ? (T?)parameters[2] : default;
            return success;
        }

        // Try without format provider
        var tryParseMethodSimple = typeof(T).GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(T).MakeByRefType() },
            null);

        if (tryParseMethodSimple != null)
        {
            var parameters = new object?[] { value, null };
            var success = (bool)tryParseMethodSimple.Invoke(null, parameters)!;
            result = success ? (T?)parameters[1] : default;
            return success;
        }

        result = default;
        return false;
    }
}
