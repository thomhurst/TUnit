namespace TUnit.Assertions.AssertConditions.Generic;

public class Property : Property<object>
{
    public Property(string name) : base(name)
    {
    }
}

public class Property<TExpected>
{
    private readonly string _name;

    public Property(string name)
    {
        _name = name;
    }
    
    public AssertCondition<object, TExpected> EqualTo(TExpected expected)
    {
        return new PropertyEqualsAssertCondition<TExpected>(_name, expected);
    }
}