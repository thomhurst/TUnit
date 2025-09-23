namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class GenericNotEqualToAssertionBuilderWrapper<TActual> : AssertionBuilder<TActual>
{
    internal GenericNotEqualToAssertionBuilderWrapper(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.Actual, assertionBuilder.ActualExpression)
    {
        // Copy the assertion chain from the original builder
        foreach (var assertion in assertionBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }
}
