using System.Numerics;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions.AssertConditions.ConditionEntries.Static;

public static partial class Is
{
    public static AssertCondition<long, long> Zero => new EqualsAssertCondition<long, long>([], null, 0);
    internal static AssertCondition<long, long> ZeroInternal(IReadOnlyCollection<AssertCondition<long, long>> nestedConditions, NestedConditionsOperator? @operator) 
        => new EqualsAssertCondition<long, long>(nestedConditions, @operator, 0);
    
    public static AssertCondition<T, T> GreaterThan<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>([], null, expected);
    }
    
    public static AssertCondition<T, T> GreaterThanOrEqualTo<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanOrEqualToAssertCondition<T, T>([], null, expected);
    }
    
    public static AssertCondition<T, T> LessThan<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>([], null, expected);
    }
    
    public static AssertCondition<T, T> LessThanOrEqualTo<T>(T expected) where T : INumber<T>
    {
        return new LessThanOrEqualToAssertCondition<T, T>([], null, expected);
    }
    
    public static AssertCondition<T, T> Even<T>() where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new IsEvenAssertCondition<T>([], null, default);
    }
    
    public static AssertCondition<T, T> Odd<T>() where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new IsOddAssertCondition<T>([], null, default);
    }
}