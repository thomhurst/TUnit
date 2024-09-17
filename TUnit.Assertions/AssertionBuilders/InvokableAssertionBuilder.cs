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
    string? GetExpression();
}

public class InvokableAssertionBuilder<TActual, TAnd, TOr> : 
    AssertionBuilder<TActual, TAnd, TOr>, IInvokableAssertionBuilder where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<Task<AssertionData<TActual>>> _assertionDataDelegate;
    public TAnd And => TAnd.Create(_assertionDataDelegate, AppendConnector(ChainType.And));
    public TOr Or => TOr.Create(_assertionDataDelegate, AppendConnector(ChainType.Or));
    
    internal InvokableAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual> assertionBuilder) : base(assertionDataDelegate, assertionBuilder.RawActualExpression!, assertionBuilder.AssertionMessage, assertionBuilder.ExpressionBuilder, assertionBuilder.Assertions)
    {
        _assertionDataDelegate = assertionDataDelegate;
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
                     {GetExpression()}
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
    public string? GetExpression()
    {
        return ExpressionBuilder?.ToString();
    }
}