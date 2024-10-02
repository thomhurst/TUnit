﻿using FluentAssertions;
using ModularPipelines.Context;
using Polly.Retry;

namespace TUnit.Pipeline.Modules.Tests;

public class DependsOnAndNotInParallelTests : TestModule
{
    protected override AsyncRetryPolicy<TestResult?> RetryPolicy => CreateRetryPolicy(3);
    
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/DependsOnAndNotInParallelTests/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(2),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0),

            ], cancellationToken);
    }
}