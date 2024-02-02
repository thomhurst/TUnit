using System.Numerics;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions;

public static class OrIsExtensions
{
    public static AssertConditionOr<T> Zero<T>(this OrIs<T> orIs) where T : INumber<T> =>
        new(orIs.OtherAssertCondition, new EqualsAssertCondition<T, T>(orIs.OtherAssertCondition.AssertionBuilder, T.Zero));
    
    public static AssertConditionOr<T> GreaterThan<T>(this OrIs<T> orIs, T expected) where T : INumber<T>
    {
        return new(orIs.OtherAssertCondition, new GreaterThanAssertCondition<T, T>(orIs.OtherAssertCondition.AssertionBuilder,  expected));
    }
    
    public static AssertConditionOr<T> GreaterThanOrEqualTo<T>(this OrIs<T> orIs, T expected) where T : INumber<T>
    {
        return new(orIs.OtherAssertCondition, new GreaterThanOrEqualToAssertCondition<T, T>(orIs.OtherAssertCondition.AssertionBuilder,  expected));
    }
    
    public static AssertConditionOr<T> LessThan<T>(this OrIs<T> orIs, T expected) where T : INumber<T>
    {
        return new(orIs.OtherAssertCondition, new GreaterThanAssertCondition<T, T>(orIs.OtherAssertCondition.AssertionBuilder,  expected));
    }
    
    public static AssertConditionOr<T> LessThanOrEqualTo<T>(this OrIs<T> orIs, T expected) where T : INumber<T>
    {
        return new(orIs.OtherAssertCondition, new LessThanOrEqualToAssertCondition<T, T>(orIs.OtherAssertCondition.AssertionBuilder,  expected));
    }
    
    public static AssertConditionOr<T> Even<T>(this OrIs<T> orIs) where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new(orIs.OtherAssertCondition, new IsEvenAssertCondition<T>(orIs.OtherAssertCondition.AssertionBuilder));
    }
    
    public static AssertConditionOr<T> Odd<T>(this OrIs<T> orIs) where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new(orIs.OtherAssertCondition, new IsOddAssertCondition<T>(orIs.OtherAssertCondition.AssertionBuilder));
    }
}