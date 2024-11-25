﻿using FluentAssertions;

namespace TUnit.Engine.Tests;

public class CustomDisplayNameTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/CustomDisplayNameTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(5),
                result => result.ResultSummary.Counters.Passed.Should().Be(5),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}