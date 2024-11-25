﻿using FluentAssertions;
using TUnit.Engine.Tests.Extensions;

namespace TUnit.Engine.Tests;

public class TestSessionAfterHookTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter("/*/*/TestSessionAfterTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(1),
                result => result.ResultSummary.Counters.Passed.Should().Be(1),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),
                _ => FindFile(x => x.Name.Contains("TestSessionAfterTests") && x.Extension == ".txt").AssertExists()
            ]);
    }
}
