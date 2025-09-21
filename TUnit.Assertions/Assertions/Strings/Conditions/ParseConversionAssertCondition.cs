using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Strings.Conditions;

public class ParseConversionAssertCondition<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TTarget>
    : ConvertToAssertCondition<string?, TTarget>
{
    private readonly IFormatProvider? _formatProvider;

    public ParseConversionAssertCondition(IFormatProvider? formatProvider = null)
    {
        _formatProvider = formatProvider;
    }

    protected internal override string GetExpectation() => $"to parse into {typeof(TTarget).Name}";

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "The generic type is preserved through the DynamicallyAccessedMembers attribute")]
    public override ValueTask<(AssertionResult, TTarget?)> ConvertValue(string? value)
    {
        if (value is null)
        {
            return new ValueTask<(AssertionResult, TTarget?)>((
                AssertionResult.FailIf(true, "Cannot parse null string"),
                default(TTarget)
            ));
        }

        var targetType = typeof(TTarget);
        object? parsedValue = null;
        bool success = false;

#if NET7_0_OR_GREATER
        // Try IParsable<T> first
        var iParsableType = typeof(IParsable<>).MakeGenericType(targetType);
        if (targetType.GetInterfaces().Any(i => i == iParsableType))
        {
            var tryParseMethod = targetType.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                _formatProvider != null
                    ? [typeof(string), typeof(IFormatProvider), targetType.MakeByRefType()]
                    : [typeof(string), targetType.MakeByRefType()],
                null);

            if (tryParseMethod != null)
            {
                var parameters = _formatProvider != null
                    ? new object?[] { value, _formatProvider, null }
                    : new object?[] { value, null };

                success = (bool)tryParseMethod.Invoke(null, parameters)!;
                if (success)
                {
                    parsedValue = parameters[^1];
                    return new ValueTask<(AssertionResult, TTarget?)>((
                        AssertionResult.Passed,
                        (TTarget?)parsedValue
                    ));
                }
            }
        }

        // Try ISpanParsable<T>
        // Note: Skipping ISpanParsable due to ReadOnlySpan boxing limitations
        // IParsable should handle most modern types
#endif

        // Fallback to traditional TryParse method
        if (!success)
        {
            var traditionalTryParse = targetType.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), targetType.MakeByRefType()],
                null);

            if (traditionalTryParse != null)
            {
                var parameters = new object?[] { value, null };
                success = (bool)traditionalTryParse.Invoke(null, parameters)!;
                
                if (success)
                {
                    parsedValue = parameters[^1];
                    return new ValueTask<(AssertionResult, TTarget?)>((
                        AssertionResult.Passed,
                        (TTarget?)parsedValue
                    ));
                }
            }
        }

        // Try TryParse with IFormatProvider for types like DateTime, DateTimeOffset
        if (!success && _formatProvider != null)
        {
            var tryParseWithProvider = targetType.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), targetType.MakeByRefType()],
                null);

            if (tryParseWithProvider != null)
            {
                var parameters = new object?[] { value, _formatProvider, DateTimeStyles.None, null };
                success = (bool)tryParseWithProvider.Invoke(null, parameters)!;
                
                if (success)
                {
                    parsedValue = parameters[^1];
                    return new ValueTask<(AssertionResult, TTarget?)>((
                        AssertionResult.Passed,
                        (TTarget?)parsedValue
                    ));
                }
            }

            // Try without DateTimeStyles
            tryParseWithProvider = targetType.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(IFormatProvider), targetType.MakeByRefType()],
                null);

            if (tryParseWithProvider != null)
            {
                var parameters = new object?[] { value, _formatProvider, null };
                success = (bool)tryParseWithProvider.Invoke(null, parameters)!;
                
                if (success)
                {
                    parsedValue = parameters[^1];
                    return new ValueTask<(AssertionResult, TTarget?)>((
                        AssertionResult.Passed,
                        (TTarget?)parsedValue
                    ));
                }
            }
        }

        var errorMessage = $"Failed to parse '{value}' into {targetType.Name}";
        if (_formatProvider != null)
        {
            errorMessage += $" with format provider {_formatProvider}";
        }

        return new ValueTask<(AssertionResult, TTarget?)>((
            AssertionResult.FailIf(true, errorMessage),
            default(TTarget)
        ));
    }
}