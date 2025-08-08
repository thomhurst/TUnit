using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static class DelegateIsNotExtensions
{
    public static NotNullAssertionBuilderWrapper<Func<T>> IsNotNull<T>(this IValueSource<Func<T>?> valueSource)
    {
        return new NotNullAssertionBuilderWrapper<Func<T>>(
            valueSource.RegisterConversionAssertion(
                new NotNullExpectedValueAssertCondition<Func<T>?>(), 
                []));
    }
    
    public static NotNullAssertionBuilderWrapper<Action> IsNotNull(this IValueSource<Action?> valueSource)
    {
        return new NotNullAssertionBuilderWrapper<Action>(
            valueSource.RegisterConversionAssertion(
                new NotNullExpectedValueAssertCondition<Action?>(), 
                []));
    }
}