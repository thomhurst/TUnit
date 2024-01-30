namespace TUnit.Assertions.AssertConditions.Generic;

public class PropertyOrMethod
{
    private readonly string _name;

    public PropertyOrMethod(string name)
    {
        _name = name;
    }
    
    internal AssertCondition<object, TExpected> EqualTo<TExpected>(TExpected expected)
    {
        return new PropertyOrMethodEqualsAssertCondition<TExpected>(_name, expected);
    }
}