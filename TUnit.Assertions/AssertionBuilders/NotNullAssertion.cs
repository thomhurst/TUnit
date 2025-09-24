namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for not-null assertions
/// </summary>
public class NotNullAssertion<TActual> : FluentAssertionBase<TActual, NotNullAssertion<TActual>>
    where TActual : class
{
    internal NotNullAssertion(AssertionBuilder<TActual> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    // This class is ready for future customization methods
    // For now, it provides the base fluent chaining functionality
}