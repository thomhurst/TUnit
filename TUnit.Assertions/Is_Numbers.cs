using System.Numerics;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions;

public static partial class Is
{
    public static AssertCondition<long> Zero => new EqualsAssertCondition<long, long>(0);
    
    public static AssertCondition<T> GreaterThan<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T> GreaterThanOrEqualTo<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanOrEqualToAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T> LessThan<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T> LessThanOrEqualTo<T>(T expected) where T : INumber<T>
    {
        return new LessThanOrEqualToAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T> Even<T>() where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new IsEvenAssertCondition<T>();
    }
    
    public static AssertCondition<T> Odd<T>() where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new IsOddAssertCondition<T>();
    }
}