namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class GenericEqualToAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual>
{
    internal GenericEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }
}