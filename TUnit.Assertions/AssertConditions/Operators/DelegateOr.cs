using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual>(AssertionBuilder<TActual> assertionBuilder) : IDelegateSource<TActual>
{
    public static DelegateOr<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new DelegateOr<TActual>(assertionBuilder);
    }

    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new OrAssertionBuilder<TActual>(assertionBuilder);
}