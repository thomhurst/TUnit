using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Base;

namespace TUnit.Assertions.Extensions;

public static class SourceExtensions
{
    // Extension methods to make IValueSource compatible with AssertionBuilder methods
    public static async ValueTask<AssertionData> GetAssertionData<TActual>(this IValueSource<TActual> source)
    {
        if (source is AssertionBuilder<TActual> builder)
        {
            return await builder.GetAssertionData();
        }
        
        // For non-AssertionBuilder sources, we need to extract the data differently
        // This is a temporary implementation until we fully migrate
        return (source, null, source.ActualExpression, DateTimeOffset.Now, DateTimeOffset.Now);
    }
    
    public static async Task ProcessAssertionsAsync<TActual>(this IValueSource<TActual> source, AssertionData data)
    {
        if (source is AssertionBuilder<TActual> builder)
        {
            await builder.ProcessAssertionsAsync(data);
        }
        // For non-AssertionBuilder sources, processing happens differently
        // This is a temporary implementation until we fully migrate
    }
    
    public static async Task ProcessAssertionsAsync<TActual>(this IValueSource<TActual> source)
    {
        var data = await source.GetAssertionData();
        await source.ProcessAssertionsAsync(data);
    }
    
    // Extension methods for IDelegateSource
    public static async ValueTask<AssertionData> GetAssertionData(this IDelegateSource source)
    {
        if (source is AssertionBuilder<object?> builder)
        {
            return await builder.GetAssertionData();
        }
        
        // For non-AssertionBuilder sources
        return (null, null, source.ActualExpression, DateTimeOffset.Now, DateTimeOffset.Now);
    }
    
    public static async Task ProcessAssertionsAsync(this IDelegateSource source, AssertionData data)
    {
        if (source is AssertionBuilder<object?> builder)
        {
            await builder.ProcessAssertionsAsync(data);
        }
        // For non-AssertionBuilder sources, processing happens differently
    }
    
    public static async Task ProcessAssertionsAsync(this IDelegateSource source)
    {
        var data = await source.GetAssertionData();
        await source.ProcessAssertionsAsync(data);
    }
    public static AssertionBuilder<TActual> RegisterAssertion<TActual>(this IValueSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        if (!string.IsNullOrEmpty(caller))
        {
            source.AppendExpression(BuildExpression(caller, argumentExpressions));
        }

        source.WithAssertion(assertCondition);
        
        // Return as AssertionBuilder if it is one
        if (source is AssertionBuilder<TActual> builder)
        {
            return builder;
        }
        
        // For other IValueSource implementations, create a new AssertionBuilder
        // but preserve the chain if the source is an Assertion
        if (source is Assertion<TActual> assertion)
        {
            // Use the existing chain to maintain Because functionality
            var chain = assertion.GetChain();
            var newBuilder = new AssertionBuilder<TActual>(default(TActual)!, source.ActualExpression, chain);
            return newBuilder;
        }
        
        // Fall back to creating a new AssertionBuilder with copied assertions
        var fallbackBuilder = new AssertionBuilder<TActual>(default(TActual)!, source.ActualExpression);
        foreach (var condition in source.GetAssertions())
        {
            fallbackBuilder.WithAssertion(condition);
        }
        
        return fallbackBuilder;
    }

    public static AssertionBuilder<TToType> RegisterConversionAssertion<TFromType, TToType>(this IValueSource<TFromType> source,
        ConvertToAssertCondition<TFromType, TToType> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        // For now, return a simple wrapper - this needs proper implementation
        // when we fully remove AssertionBuilder
        return new AssertionBuilder<TToType>(default(TToType)!, source.ActualExpression);
    }

    public static AssertionBuilder<object?> RegisterAssertion<TActual>(this IDelegateSource delegateSource,
        BaseAssertCondition<TActual> assertCondition, string?[] argumentExpressions, [CallerMemberName] string? caller = null)
    {
        if (!string.IsNullOrEmpty(caller))
        {
            delegateSource.AppendExpression(BuildExpression(caller, argumentExpressions));
        }

        delegateSource.WithAssertion(assertCondition);
        
        // Return as AssertionBuilder if it is one
        if (delegateSource is AssertionBuilder<object?> builder)
        {
            return builder;
        }
        
        // For other IDelegateSource implementations, create a new AssertionBuilder
        // but preserve the chain if the source is an Assertion
        if (delegateSource is Assertion<object?> assertion)
        {
            // Use the existing chain to maintain Because functionality
            var chain = assertion.GetChain();
            var newBuilder = new AssertionBuilder<object?>(default(object)!, delegateSource.ActualExpression, chain);
            return newBuilder;
        }
        
        // Fall back to creating a new AssertionBuilder with copied assertions
        var fallbackBuilder = new AssertionBuilder<object?>(default(object)!, delegateSource.ActualExpression);
        foreach (var condition in delegateSource.GetAssertions())
        {
            fallbackBuilder.WithAssertion(condition);
        }
        
        return fallbackBuilder;
    }

    public static AssertionBuilder<TToType> RegisterConversionAssertion<TToType>(this IDelegateSource source) where TToType : Exception
    {
        // Simplified for now - needs proper implementation
        return new AssertionBuilder<TToType>(default(TToType)!, source.ActualExpression);
    }

    private static string BuildExpression(string? caller, string?[] arguments)
    {
        var sb = new StringBuilder();
        sb.Append(caller);
        sb.Append('(');

        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            sb.Append(argument);

            if (index < arguments.Length - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(')');
        return sb.ToString();
    }
}