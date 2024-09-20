﻿using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class DependsOnTestsWithProceedOnFailure : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/DependsOnTestsWithProceedOnFailure/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(1),
                result => result.Failed.Should().Be(1),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}