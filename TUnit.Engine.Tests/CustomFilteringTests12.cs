﻿using Shouldly;

namespace TUnit.Engine.Tests;

public class CaptureOutputTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/CaptureOutputTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(80),
                result => result.ResultSummary.Counters.Passed.ShouldBe(80),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}