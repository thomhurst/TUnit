using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public static class Has
{
    public static Property<object, int> Count => new("Count");
    public static Property<object, int> Length => new("Length");
    public static Property<object> Value => new("Value");
    
    public static Property<object> Property(string name) => new(name);
    public static Property<object, T> Property<T>(string name) => new(name);
}