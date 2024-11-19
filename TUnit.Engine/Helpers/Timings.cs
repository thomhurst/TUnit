﻿using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Helpers;

internal static class Timings
{
    public static async Task Record(string name, TestContext context, Func<Task> action)
    {
        var start = DateTimeOffset.Now;

        try
        {
            await action();
        }
        finally
        {
            var end = DateTimeOffset.Now;
            
            lock (context.Lock)
            {
                context.Timings.Add(new Timing(name, start, end));
            }
        }
    }  
    
    public static TimingProperty GetTimingProperty(TestContext testContext, DateTimeOffset overallStart)
    {
        var end = DateTimeOffset.Now;

        lock (testContext.Lock)
        {
            var stepTimings = testContext.Timings.Select(x =>
                new StepTimingInfo(x.StepName, string.Empty, new TimingInfo(x.Start, x.End, x.Duration)));

            return new TimingProperty(new TimingInfo(overallStart, end, end - overallStart), [.. stepTimings]);
        }
    }
}