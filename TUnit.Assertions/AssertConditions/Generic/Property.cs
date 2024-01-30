namespace TUnit.Assertions.AssertConditions.Generic;

public class Property
{
    private readonly string _name;

    public Property(string name)
    {
        _name = name;
    }
    
    public AssertCondition<object, TExpected> EqualTo<TExpected>(TExpected expected)
    {
        return new PropertyEqualsAssertCondition<TExpected>(_name, expected);
    }
}