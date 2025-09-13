using System;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Enums.Conditions;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Enum.HasFlag is an instance method that returns boolean
[CreateAssertion<Enum>( nameof(Enum.HasFlag))]
[CreateAssertion<Enum>( nameof(Enum.HasFlag), CustomName = "DoesNotHaveFlag", NegateLogic = true)]
public static partial class EnumAssertionExtensions
{
    // Manual extension methods for IsDefined since it requires generic constraints
    public static InvokableValueAssertionBuilder<TEnum> IsDefined<TEnum>(this IValueSource<TEnum> valueSource)
        where TEnum : struct, Enum
    {
        return valueSource.RegisterAssertion(new EnumIsDefinedAssertCondition<TEnum>(), []);
    }

    public static InvokableValueAssertionBuilder<TEnum> IsNotDefined<TEnum>(this IValueSource<TEnum> valueSource)
        where TEnum : struct, Enum
    {
        return valueSource.RegisterAssertion(new EnumIsNotDefinedAssertCondition<TEnum>(), []);
    }
}
