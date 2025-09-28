using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions.Comparable;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean not-between assertion - no inheritance, just configuration
/// </summary>
public class NotBetweenAssertion<TActual> where TActual : IComparable<TActual>
{
    private readonly IValueSource<TActual> _source;
    private readonly TActual _minimum;
    private readonly TActual _maximum;
    private readonly string?[] _expressions;
    
    // Configuration
    private bool _inclusive = false;

    internal NotBetweenAssertion(IValueSource<TActual> source, TActual minimum, TActual maximum, string?[] expressions)
    {
        _source = source;
        _minimum = minimum;
        _maximum = maximum;
        _expressions = expressions;
    }

    public NotBetweenAssertion<TActual> Inclusive()
    {
        _inclusive = true;
        return this;
    }
    
    public NotBetweenAssertion<TActual> Exclusive()
    {
        _inclusive = false;
        return this;
    }

    public TaskAwaiter GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    private async Task ExecuteAsync()
    {
        // Create condition with all configuration
        var condition = new NotBetweenAssertCondition<TActual>(_minimum, _maximum);
        
        // Apply configuration
        if (_inclusive)
            condition.Inclusive();
        else
            condition.Exclusive();
        
        // Register and execute
        var builder = _source.RegisterAssertion(condition, _expressions);
        var data = await builder.GetAssertionData();
        await builder.ProcessAssertionsAsync(data);
    }
}