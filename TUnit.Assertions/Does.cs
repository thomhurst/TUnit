using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public class Does<T>
{
    protected AssertionBuilder<T> AssertionBuilder { get; }

    public Does(AssertionBuilder<T> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public Property<T, int> Count => new(AssertionBuilder, "Count");
    public Property<T, int> Length => new(AssertionBuilder, "Length");
    public Property<T> Value => new(AssertionBuilder, "Value");
    
    public Property<T> Property(string name) => new(AssertionBuilder, name);
    public Property<T, TPropertyType> Property<TPropertyType>(string name) => new(AssertionBuilder, name);
}