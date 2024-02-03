using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public class Has<T>
{
    protected internal AssertionBuilder<T> AssertionBuilder { get; }

    public Has(AssertionBuilder<T> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public Property<T> Property(string name) => new(AssertionBuilder, name);
    public Property<T, TPropertyType> Property<TPropertyType>(string name) => new(AssertionBuilder, name);
}