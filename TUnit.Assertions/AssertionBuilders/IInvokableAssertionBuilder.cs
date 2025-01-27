﻿using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

public interface IInvokableAssertionBuilder : ISource
{
    TaskAwaiter GetAwaiter();
    string? GetExpression();
}