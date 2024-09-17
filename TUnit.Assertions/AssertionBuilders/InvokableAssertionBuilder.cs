using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertionBuilder
{
    Task ProcessAssertionsAsync();
    IAsyncEnumerable<BaseAssertCondition> GetFailures();
    TaskAwaiter GetAwaiter() => ProcessAssertionsAsync().GetAwaiter();
}

public class InvokableAssertionBuilder<TActual, TAnd, TOr> : 
    AssertionBuilder<TActual, TAnd, TOr>, IInvokableAssertionBuilder where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    public TAnd And { get; }
    public TOr Or { get; }
    
    internal InvokableAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual> assertionBuilder) : base(assertionDataDelegate, assertionBuilder.RawActualExpression!, assertionBuilder.AssertionMessage, assertionBuilder.ExpressionBuilder)
    {
        foreach (var assertion in assertionBuilder.Assertions)
        {
            Assertions.Push(assertion);
        }

        And = TAnd.Create(assertionDataDelegate, this);
        Or = TOr.Create(assertionDataDelegate, this);
    }

    public async Task ProcessAssertionsAsync()
    {
        var currentAssertionScope = AssertionScope.GetCurrentAssertionScope();
        
        if (currentAssertionScope != null)
        {
            currentAssertionScope.Add(this);
            return;
        }

        var assertionData = await AssertionDataDelegate();
        
        foreach (var assertion in Assertions)
        {
            if (!assertion.Assert(assertionData))
            {
                throw new AssertionException(
                    $"""
                     {ExpressionBuilder}
                     {assertion.Message}
                     """
                );
            }
        }
    }

    public async IAsyncEnumerable<BaseAssertCondition> GetFailures()
    {
        var assertionData = await AssertionDataDelegate();
        
        foreach (var assertion in Assertions)
        {
            if (!assertion.Assert(assertionData))
            {
                yield return assertion;
            }
        }
    }

    public TaskAwaiter GetAwaiter() => ProcessAssertionsAsync().GetAwaiter();
}