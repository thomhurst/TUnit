using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Generic extension methods for type-independent assertions
/// </summary>
public static class GenericAssertionExtensions
{
    // === IsDefault/IsNotDefault for any type ===
    public static CustomAssertion<T> IsDefault<T>(this ValueAssertionBuilder<T> builder)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual => EqualityComparer<T>.Default.Equals(actual, default!),
            $"Expected value to be default({typeof(T).Name}) but was {{ActualValue}}");
    }

    public static CustomAssertion<T> IsNotDefault<T>(this ValueAssertionBuilder<T> builder)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual => !EqualityComparer<T>.Default.Equals(actual, default!),
            $"Expected value to not be default({typeof(T).Name}) but it was");
    }

    // === For DualAssertionBuilder ===
    public static CustomAssertion<T> IsDefault<T>(this DualAssertionBuilder<T> builder)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual => EqualityComparer<T>.Default.Equals(actual, default!),
            $"Expected value to be default({typeof(T).Name}) but was {{ActualValue}}");
    }

    public static CustomAssertion<T> IsNotDefault<T>(this DualAssertionBuilder<T> builder)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual => !EqualityComparer<T>.Default.Equals(actual, default!),
            $"Expected value to not be default({typeof(T).Name}) but it was");
    }

    // === HasMember for checking if an object has a property or field ===
    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "HasMember is used for testing and reflection is acceptable")]
    public static CustomAssertion<T> HasMember<T>(this ValueAssertionBuilder<T> builder, string memberName)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                var type = actual.GetType();
                return type.GetProperty(memberName) != null || type.GetField(memberName) != null;
            },
            $"Expected object to have member '{memberName}'");
    }

    // HasMember overload that accepts a member selector expression and returns a builder for the member value
    public static MemberAssertion<T, TMember> HasMember<T, TMember>(
        this ValueAssertionBuilder<T> builder,
        Expression<Func<T, TMember>> memberSelector)
    {
        return new MemberAssertion<T, TMember>(builder.ActualValueProvider, memberSelector);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "DoesNotHaveMember is used for testing and reflection is acceptable")]
    public static CustomAssertion<T> DoesNotHaveMember<T>(this ValueAssertionBuilder<T> builder, string memberName)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                var type = actual.GetType();
                return type.GetProperty(memberName) == null && type.GetField(memberName) == null;
            },
            $"Expected object to not have member '{memberName}'");
    }

    // === For DualAssertionBuilder ===
    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "HasMember is used for testing and reflection is acceptable")]
    public static CustomAssertion<T> HasMember<T>(this DualAssertionBuilder<T> builder, string memberName)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                var type = actual.GetType();
                return type.GetProperty(memberName) != null || type.GetField(memberName) != null;
            },
            $"Expected object to have member '{memberName}'");
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "DoesNotHaveMember is used for testing and reflection is acceptable")]
    public static CustomAssertion<T> DoesNotHaveMember<T>(this DualAssertionBuilder<T> builder, string memberName)
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                var type = actual.GetType();
                return type.GetProperty(memberName) == null && type.GetField(memberName) == null;
            },
            $"Expected object to not have member '{memberName}'");
    }
}