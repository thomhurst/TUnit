﻿using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder
    : AssertionBuilder<object?>,
        IDelegateSource<object?>
{
    internal DelegateAssertionBuilder(Action action, string expressionBuilder) : base(action.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    AssertionBuilder<object?> ISource<object?>.AssertionBuilder => this;
}