using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean single item assertion - no inheritance, just configuration
/// </summary>
public class SingleItemAssertion<TEnumerable, TInner>
{
    private readonly AssertionBuilder<TEnumerable> _builder;

    internal SingleItemAssertion(AssertionBuilder<TEnumerable> builder)
    {
        _builder = builder;
    }

    public AssertionBuilder<TEnumerable> And => _builder.And;
    public AssertionBuilder<TEnumerable> Or => _builder.Or;
    
    public TaskAwaiter GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    private async Task ExecuteAsync()
    {
        // Since this assertion doesn't add any conditions and just wraps the builder,
        // we can directly process the assertions
        var data = await _builder.GetAssertionData();
        await _builder.ProcessAssertionsAsync(data);
    }
}
