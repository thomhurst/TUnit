using System.Numerics;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions.Extensions;

public static class AndIsExtensions
{
    public static AssertConditionAnd<T> Zero<T>(this AndIs<T> andIs) where T : INumber<T> =>
        new(andIs.OtherAssertCondition, new EqualsAssertCondition<T, T>(T.Zero));
    
    public static AssertConditionAnd<T> GreaterThan<T>(this AndIs<T> andIs, T expected) where T : INumber<T>
    {
        return new(andIs.OtherAssertCondition, new GreaterThanAssertCondition<T, T>(expected));
    }
    
    public static AssertConditionAnd<T> GreaterThanOrEqualTo<T>(this AndIs<T> andIs, T expected) where T : INumber<T>
    {
        return new(andIs.OtherAssertCondition, new GreaterThanOrEqualToAssertCondition<T, T>(expected));
    }
    
    public static AssertConditionAnd<T> LessThan<T>(this AndIs<T> andIs, T expected) where T : INumber<T>
    {
        return new(andIs.OtherAssertCondition, new GreaterThanAssertCondition<T, T>(expected));
    }
    
    public static AssertConditionAnd<T> LessThanOrEqualTo<T>(this AndIs<T> andIs, T expected) where T : INumber<T>
    {
        return new(andIs.OtherAssertCondition, new LessThanOrEqualToAssertCondition<T, T>(expected));
    }
    
    public static AssertConditionAnd<T> Even<T>(this AndIs<T> andIs) where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new(andIs.OtherAssertCondition, new IsEvenAssertCondition<T>());
    }
    
    public static AssertConditionAnd<T> Odd<T>(this AndIs<T> andIs) where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new(andIs.OtherAssertCondition, new IsOddAssertCondition<T>());
    }
}