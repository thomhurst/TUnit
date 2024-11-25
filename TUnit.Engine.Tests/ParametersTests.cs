﻿using FluentAssertions;

namespace TUnit.Engine.Tests;

public class ParametersTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ParametersTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(1),
                result => result.ResultSummary.Counters.Passed.Should().Be(1),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ],
            new RunOptions
            {
                AdditionalArguments = ["--test-parameter", "TestParam1=TestParam1Value"]
            });
    }
}