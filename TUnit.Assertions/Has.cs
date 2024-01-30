using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public static class Has
{
    public static Property Count => new("Count");
    public static Property Length => new("Length");
    public static Property Value => new("Value");
}