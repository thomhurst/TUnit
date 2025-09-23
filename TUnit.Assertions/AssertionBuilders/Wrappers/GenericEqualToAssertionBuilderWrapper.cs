namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class GenericEqualToAssertionBuilderWrapper<TActual> : AssertionBuilder<TActual>
{
    internal GenericEqualToAssertionBuilderWrapper(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.Actual, assertionBuilder.ActualExpression)
    {
        // Copy the assertion chain from the original builder
        foreach (var assertion in assertionBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }
}
