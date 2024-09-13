﻿using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;

namespace TUnit.Pipeline.Modules.Tests;

public class ClassHooksExecutionCountTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/ClassHooksExecutionCountTests/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(5),
                result => result.Passed.Should().Be(5),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0),
            ], cancellationToken);
    }
}