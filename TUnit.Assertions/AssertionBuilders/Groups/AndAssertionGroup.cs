using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders.Groups;

public class AndAssertionGroup<TActual, TAssertionBuilder> : AssertionGroup<TActual, TAssertionBuilder>
    where TAssertionBuilder : AssertionBuilder
{
    private readonly List<BaseAssertCondition> _conditions = [];
    private AssertionBuilder<TActual>? _assertionBuilder;

    internal AndAssertionGroup(Func<TAssertionBuilder, AssertionBuilder<TActual>> initialAssert, Func<TAssertionBuilder, AssertionBuilder<TActual>> assert, TAssertionBuilder assertionBuilder) : base(assertionBuilder)
    {
        Push(assertionBuilder, initialAssert);
        Push(assertionBuilder, assert);
    }

    public AndAssertionGroup<TActual, TAssertionBuilder> And(Func<TAssertionBuilder, AssertionBuilder<TActual>> assert)
    {
        Push(AssertionBuilder, assert);
        return this;
    }

    public override TaskAwaiter<TActual?> GetAwaiter()
    {
        return GetResult().GetAwaiter();
    }

    private async Task<TActual?> GetResult()
    {
        // Create a combined AND condition from all collected conditions
        BaseAssertCondition? combinedCondition = null;
        foreach (var condition in _conditions)
        {
            if (combinedCondition == null)
            {
                combinedCondition = condition;
            }
            else
            {
                combinedCondition = new AndAssertCondition(combinedCondition, condition);
            }
        }

        if (combinedCondition != null && _assertionBuilder != null)
        {
            // Clear existing assertions and add the combined one
            var newBuilder = new AssertionBuilder<TActual>(_assertionBuilder.Actual, _assertionBuilder.ActualExpression);
            ((ISource)newBuilder).WithAssertion(combinedCondition);
            
            var data = await newBuilder.GetAssertionData();
            await newBuilder.ProcessAssertionsAsync(data);
            return (TActual?)data.Result;
        }

        return default;
    }

    private void Push(TAssertionBuilder assertionBuilder, Func<TAssertionBuilder, AssertionBuilder<TActual>> assert)
    {
        var builder = assert(assertionBuilder);
        _assertionBuilder = builder;
        
        // Extract the last assertion from the builder
        var assertions = builder.GetAssertions().ToList();
        if (assertions.Count > 0)
        {
            _conditions.Add(assertions.Last());
        }
    }
}