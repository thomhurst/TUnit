using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean not-null assertion - no inheritance, just configuration
/// </summary>
public class NotNullAssertion<TActual>
    where TActual : class
{
    private readonly IValueSource<TActual> _source;
    private readonly string?[] _expressions;

    internal NotNullAssertion(IValueSource<TActual> source, string?[] expressions)
    {
        _source = source;
        _expressions = expressions;
    }

    public TaskAwaiter<TActual> GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    private async Task<TActual> ExecuteAsync()
    {
        // Create condition
        var condition = new NotNullExpectedValueAssertCondition<TActual>();
        
        // Register and execute - this is a conversion assertion since it converts nullable to non-nullable
        var builder = _source.RegisterConversionAssertion(condition, _expressions);
        var data = await builder.GetAssertionData();
        await builder.ProcessAssertionsAsync(data);
        
        // Return the non-null value
        return (TActual)data.Result!;
    }

    // Additional fluent methods can be added here for future customization
    
    // Implicit conversion to AssertionBuilder for compatibility
    public static implicit operator AssertionBuilder<TActual>(NotNullAssertion<TActual> assertion)
    {
        var condition = new NotNullExpectedValueAssertCondition<TActual>();
        assertion._source.RegisterConversionAssertion(condition, assertion._expressions);
        
        // Return the source wrapped as AssertionBuilder for compatibility
        if (assertion._source is AssertionBuilder<TActual> builder)
        {
            return builder;
        }
        
        // Create a new AssertionBuilder for compatibility
        return new AssertionBuilder<TActual>(default(TActual)!, assertion._source.ActualExpression);
    }
}
