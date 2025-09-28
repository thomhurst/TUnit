using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Extensions;

public static class CustomAssertionExtensions
{
    // Extension for AssertionBuilder to support custom assertions
    public static AssertionBuilder<TActual> RegisterAssertion<TActual>(
        this AssertionBuilder<TActual> builder,
        BaseAssertCondition<TActual> condition,
        string[]? expressions = null)
    {
        return new AssertionBuilder<TActual>(async () =>
        {
            var value = await builder.ActualValueProvider();

            // Apply the condition
            var now = System.DateTimeOffset.Now;
            var conditionResult = await condition.GetAssertionResult(value, null, new AssertionMetadata { StartTime = now, EndTime = now }, null);
            if (!conditionResult.IsPassed)
            {
                throw new AssertionException(conditionResult.Message ?? "Assertion failed");
            }
            return value;
        });
    }

    // Extension for IValueSource for backward compatibility
    public static AssertionBuilder<TActual> RegisterAssertion<TActual>(
        this IValueSource<TActual> valueSource,
        BaseAssertCondition<TActual> condition,
        string[]? expressions = null)
    {
        // If valueSource is an AssertionBuilder, use it
        if (valueSource is AssertionBuilder<TActual> builder)
        {
            return builder.RegisterAssertion(condition, expressions);
        }

        // Create a new builder that will execute the assertion
        return new AssertionBuilder<TActual>(() => Task.FromResult(default(TActual)!));
    }

    // Extension for AssertionBuilder conversion assertions
    public static AssertionBuilder<TToType> RegisterConversionAssertion<TActual, TToType>(
        this AssertionBuilder<TActual> builder,
        ConvertToAssertCondition<TActual, TToType> condition,
        string[]? expressions = null)
    {
        return new AssertionBuilder<TToType>(async () =>
        {
            var value = await builder.ActualValueProvider();
            var (result, convertedValue) = await condition.ConvertValue(value);
            if (!result.IsPassed)
            {
                throw new AssertionException(result.Message ?? "Conversion failed");
            }
            return convertedValue!;
        });
    }

    // Extension for IValueSource conversion for backward compatibility
    public static AssertionBuilder<TToType> RegisterConversionAssertion<TActual, TToType>(
        this IValueSource<TActual> valueSource,
        ConvertToAssertCondition<TActual, TToType> condition,
        string[]? expressions = null)
    {
        // If valueSource is an AssertionBuilder, use it
        if (valueSource is AssertionBuilder<TActual> builder)
        {
            return builder.RegisterConversionAssertion(condition, expressions);
        }

        // Create a new builder
        return new AssertionBuilder<TToType>(() => Task.FromResult(default(TToType)!));
    }
}