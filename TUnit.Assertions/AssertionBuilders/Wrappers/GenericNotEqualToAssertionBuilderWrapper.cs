namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class GenericNotEqualToAssertionBuilderWrapper<TActual> : InvokableValueAssertion<TActual>
{
    internal GenericNotEqualToAssertionBuilderWrapper(InvokableAssertion<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }
}
