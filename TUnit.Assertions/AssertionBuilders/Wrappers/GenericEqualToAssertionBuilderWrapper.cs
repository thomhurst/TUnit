namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class GenericEqualToAssertionBuilderWrapper<TActual> : InvokableValueAssertion<TActual>
{
    internal GenericEqualToAssertionBuilderWrapper(InvokableAssertion<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }
}
