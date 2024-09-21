using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual> 
    : IDelegateSource<TActual>
{
    private readonly AssertionBuilder<TActual> _assertionBuilder;

    public DelegateOr(AssertionBuilder<TActual> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }
    
    public static DelegateOr<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new DelegateOr<TActual>(assertionBuilder);
    }

    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new OrAssertionBuilder<TActual>(_assertionBuilder);
}