﻿using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.ClassMember;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static Member<TRootObject, TPropertyType, TAnd, TOr> HasMember<TRootObject, TPropertyType, TAnd, TOr>(this IValueSource<TRootObject, TAnd, TOr> valueSource, Expression<Func<TRootObject, TPropertyType>> selector, [CallerArgumentExpression("selector")] string expression = "")
        where TAnd : IAnd<TRootObject, TAnd, TOr>
        where TOr : IOr<TRootObject, TAnd, TOr> => new(valueSource.AssertionBuilder.AppendCallerMethod([expression]), selector);
}