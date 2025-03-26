using System.Runtime.InteropServices;
using Shouldly;

namespace TUnit.Engine.Tests;

public class CultureTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 2 : 3;
        
        await RunTestsWithFilter(
            "/*/*/CultureTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expected),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expected),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}