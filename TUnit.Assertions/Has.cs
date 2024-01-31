using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public static class Has
{
    public static Property<int> Count => new("Count");
    public static Property<int> Length => new("Length");
    public static Property Value => new("Value");
    
    public static Property Property(string name) => new(name);
    public static Property<T> Property<T>(string name) => new(name);
}