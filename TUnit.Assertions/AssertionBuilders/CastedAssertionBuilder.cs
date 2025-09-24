using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

public class CastedAssertionBuilder<TActual, TCasted> : AwaitableAssertionBuilder<TCasted>, IValueSource<TCasted>
{
    private readonly AssertionBuilder<TActual> _innerBuilder;

    public CastedAssertionBuilder(AssertionBuilder<TActual> innerBuilder)
        : base(
            async () => {
                var data = await innerBuilder.GetAssertionData();
                await innerBuilder.ProcessAssertionsAsync(data);
                if (data.Result is TCasted casted)
                {
                    return casted;
                }
                return default!;
            },
            innerBuilder.ActualExpression)
    {
        _innerBuilder = innerBuilder;

        // Copy assertions from the original builder
        foreach (var assertion in innerBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }

    // Enable fluent chaining with proper type
    public new CastedAssertionBuilder<TActual, TCasted> And
    {
        get
        {
            AppendExpression("And");
            return this;
        }
    }
}