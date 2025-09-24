namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for single item assertions
/// </summary>
public class SingleItemAssertion<TEnumerable, TInner> : FluentAssertionBase<TEnumerable, SingleItemAssertion<TEnumerable, TInner>>
{
    internal SingleItemAssertion(AssertionBuilder<TEnumerable> assertionBuilder)
        : base(assertionBuilder)
    {
    }
}