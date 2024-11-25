﻿using FluentAssertions;

namespace TUnit.Engine.Tests;

public class ClassConstructorWithEnumerableTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ClassConstructorWithEnumerableTest/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(4),
                result => result.ResultSummary.Counters.Passed.Should().Be(4),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}