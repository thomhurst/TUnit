﻿using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class ConflictingDependsOnTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/ConflictingDependsOnTests/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(2),
                result => result.Skipped.Should().Be(0),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test1").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test1 > Test2 > Test1"),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test2").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test2 > Test1 > Test2"),

            ], cancellationToken);
    }
}