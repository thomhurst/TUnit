namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class GenericNotEqualToAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual>
{
    internal GenericNotEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }
}