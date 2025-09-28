using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Interfaces;

namespace TUnit.Assertions.Assertions.Base;

public abstract class Assertion<T>
{
    protected readonly IValueSource<T> _source;
    protected readonly IAssertionChain _chain;

    protected Assertion(IValueSource<T> source, IAssertionChain chain)
    {
        _source = source;
        _chain = chain ?? new AssertionChain();
    }

    protected abstract BaseAssertCondition? CreateCondition();

    public IAssertionChain GetChain() => _chain;

    public virtual Assertion<T> And => this;
    public virtual Assertion<T> Or => this;

    public virtual Assertion<T> Because(string reason, string? expression = null)
    {
        // Store the reason for later use in assertions
        return this;
    }

    public virtual async ValueTask InvokeAsync()
    {
        var condition = CreateCondition();
        if (condition != null)
        {
            _chain.AddAssertion(condition);
        }

        // Execute the chain
        var evaluator = new AssertionEvaluator();
        await evaluator.EvaluateAsync(_chain);
    }

    public ValueTaskAwaiter GetAwaiter()
    {
        return InvokeAsync().GetAwaiter();
    }
}