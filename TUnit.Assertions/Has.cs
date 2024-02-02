using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public class Has<T>
{
    internal AssertionBuilder<T> AssertionBuilder { get; }

    public Has(AssertionBuilder<T> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public Property<T, int> Count => new(AssertionBuilder, "Count");
    public Property<T, int> Length => new(AssertionBuilder, "Length");
    public Property<T> Value => new(AssertionBuilder, "Value");
    
    public Property<T> Property(string name) => new(AssertionBuilder, name);
    public Property<T, TPropertyType> Property<TPropertyType>(string name) => new(AssertionBuilder, name);
}