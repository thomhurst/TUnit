using System.Numerics;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions.Extensions;

public static class OrIsExtensions
{
    public static AssertConditionOr<T> Zero<T>(this OrIs<T> andIs) where T : INumber<T> =>
        new(andIs.OtherAssertCondition, new EqualsAssertCondition<T, T>(T.Zero));
    
    public static AssertConditionOr<T> GreaterThan<T>(this OrIs<T> andIs, T expected) where T : INumber<T>
    {
        return new(andIs.OtherAssertCondition, new GreaterThanAssertCondition<T, T>(expected));
    }
    
    public static AssertConditionOr<T> GreaterThanOrEqualTo<T>(this OrIs<T> andIs, T expected) where T : INumber<T>
    {
        return new(andIs.OtherAssertCondition, new GreaterThanOrEqualToAssertCondition<T, T>(expected));
    }
    
    public static AssertConditionOr<T> LessThan<T>(this OrIs<T> andIs, T expected) where T : INumber<T>
    {
        return new(andIs.OtherAssertCondition, new GreaterThanAssertCondition<T, T>(expected));
    }
    
    public static AssertConditionOr<T> LessThanOrEqualTo<T>(this OrIs<T> andIs, T expected) where T : INumber<T>
    {
        return new(andIs.OtherAssertCondition, new LessThanOrEqualToAssertCondition<T, T>(expected));
    }
    
    public static AssertConditionOr<T> Even<T>(this OrIs<T> andIs) where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new(andIs.OtherAssertCondition, new IsEvenAssertCondition<T>());
    }
    
    public static AssertConditionOr<T> Odd<T>(this OrIs<T> andIs) where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new(andIs.OtherAssertCondition, new IsOddAssertCondition<T>());
    }
}