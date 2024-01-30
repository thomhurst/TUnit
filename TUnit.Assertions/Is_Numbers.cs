using System.Numerics;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions;

public static partial class Is
{
    public static AssertCondition<long, long> Zero => new EqualsAssertCondition<long, long>(0);
    
    public static AssertCondition<T, T> GreaterThan<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T, T> GreaterThanOrEqualTo<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanOrEqualToAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T, T> LessThan<T>(T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T, T> LessThanOrEqualTo<T>(T expected) where T : INumber<T>
    {
        return new LessThanOrEqualToAssertCondition<T, T>(expected);
    }
    
    public static AssertCondition<T, T> Even<T>() where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new IsEvenAssertCondition<T>();
    }
    
    public static AssertCondition<T, T> Odd<T>() where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new IsOddAssertCondition<T>();
    }
}