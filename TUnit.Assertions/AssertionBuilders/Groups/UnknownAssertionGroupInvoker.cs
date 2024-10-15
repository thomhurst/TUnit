namespace TUnit.Assertions.AssertionBuilders.Groups;

public class UnknownAssertionGroupInvoker<TActual, TAssertionBuilder>(AssertionGroup<TActual, TAssertionBuilder> group)
    where TAssertionBuilder : AssertionBuilder<TActual>
{
    public AndAssertionGroupInvoker<TActual, TAssertionBuilder> And(AssertionGroup<TActual, TAssertionBuilder> otherGroup)
    {
        return new AndAssertionGroupInvoker<TActual, TAssertionBuilder>(group, otherGroup);
    }

    public OrAssertionGroupInvoker<TActual, TAssertionBuilder> Or(AssertionGroup<TActual, TAssertionBuilder> otherGroup)
    {
        return new OrAssertionGroupInvoker<TActual, TAssertionBuilder>(group, otherGroup);
    }
}