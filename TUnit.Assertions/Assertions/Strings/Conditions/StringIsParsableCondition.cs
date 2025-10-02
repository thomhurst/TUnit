using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Strings.Conditions;

public class StringIsParsableCondition<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TTarget>
    : ExpectedValueAssertCondition<string?, IFormatProvider?>
{
    public StringIsParsableCondition(IFormatProvider? formatProvider = null) 
        : base(formatProvider)
    {
    }

    protected internal override string GetExpectation() => $"to be parsable into {typeof(TTarget).Name}";

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "The generic type is preserved through the DynamicallyAccessedMembers attribute")]
    protected override ValueTask<AssertionResult> GetResult(string? actualValue, IFormatProvider? formatProvider)
    {
        if (actualValue is null)
        {
            return new ValueTask<AssertionResult>(AssertionResult.FailIf(true, "Actual string is null"));
        }

        var targetType = typeof(TTarget);

#if NET7_0_OR_GREATER
        // Try IParsable<T> first
        var iParsableType = typeof(IParsable<>).MakeGenericType(targetType);
        if (targetType.GetInterfaces().Any(i => i == iParsableType))
        {
            var tryParseMethod = targetType.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                formatProvider != null
                    ? [typeof(string), typeof(IFormatProvider), targetType.MakeByRefType()]
                    : [typeof(string), targetType.MakeByRefType()],
                null);

            if (tryParseMethod != null)
            {
                var parameters = formatProvider != null
                    ? new object?[] { actualValue, formatProvider, null }
                    : new object?[] { actualValue, null };

                var success = (bool)tryParseMethod.Invoke(null, parameters)!;
                
                return new ValueTask<AssertionResult>(AssertionResult.FailIf(!success, 
                    $"parsing failed{(formatProvider != null ? $" with format provider {formatProvider}" : "")}"));
            }
        }

        // Try ISpanParsable<T>
        var iSpanParsableType = typeof(ISpanParsable<>).MakeGenericType(targetType);
        if (targetType.GetInterfaces().Any(i => i == iSpanParsableType))
        {
            // We need to use a different approach for Span since we can't box it
            // Instead, we'll convert to string and try the regular parse method
            // This is a workaround for the boxing limitation
            // The IParsable path above should handle most cases
        }
#endif

        // Fallback to traditional TryParse method
        var traditionalTryParse = targetType.GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(string), targetType.MakeByRefType()],
            null);

        if (traditionalTryParse != null)
        {
            var parameters = new object?[] { actualValue, null };
            var success = (bool)traditionalTryParse.Invoke(null, parameters)!;
            
            return new ValueTask<AssertionResult>(AssertionResult.FailIf(!success, "parsing failed"));
        }

        // Try TryParse with IFormatProvider for types like DateTime, DateTimeOffset
        if (formatProvider != null)
        {
            var tryParseWithProvider = targetType.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), targetType.MakeByRefType()],
                null);

            if (tryParseWithProvider != null)
            {
                var parameters = new object?[] { actualValue, formatProvider, DateTimeStyles.None, null };
                var success = (bool)tryParseWithProvider.Invoke(null, parameters)!;
                
                return new ValueTask<AssertionResult>(AssertionResult.FailIf(!success, $"parsing failed with format provider {formatProvider}"));
            }

            // Try without DateTimeStyles
            tryParseWithProvider = targetType.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(IFormatProvider), targetType.MakeByRefType()],
                null);

            if (tryParseWithProvider != null)
            {
                var parameters = new object?[] { actualValue, formatProvider, null };
                var success = (bool)tryParseWithProvider.Invoke(null, parameters)!;
                
                return new ValueTask<AssertionResult>(AssertionResult.FailIf(!success, $"parsing failed with format provider {formatProvider}"));
            }
        }

        return new ValueTask<AssertionResult>(AssertionResult.FailIf(true, $"type {targetType.Name} does not support parsing"));
    }
}