using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject;

public class RepeatIndexVerificationTest
{
    private static readonly HashSet<string> SeenTestIds = new();
    private static readonly HashSet<DateTimeOffset?> SeenStartTimes = new();
    private static readonly object Lock = new();
    private static int RunCount = 0;

    [Test]
    [Repeat(3)]
    public async Task VerifyRepeatCreatesUniqueTestIds()
    {
        await Task.Yield();

        var context = TestContext.Current!;
        var testId = context.Metadata.TestDetails.TestId;
        var testStart = context.Execution.TestStart;

        lock (Lock)
        {
            RunCount++;
            Console.WriteLine($"Run #{RunCount}");
            Console.WriteLine($"Test ID: {testId}");
            Console.WriteLine($"Test Start: {testStart}");
            Console.WriteLine("---");

            // Verify unique test IDs
            if (!SeenTestIds.Add(testId))
            {
                throw new Exception($"Duplicate TestId detected: {testId}. This means RepeatIndex is not being incremented properly!");
            }

            // Track start times
            SeenStartTimes.Add(testStart);
        }
    }

    [Test]
    [DependsOn(nameof(VerifyRepeatCreatesUniqueTestIds))]
    public void VerifyAllRepeatsRan()
    {
        lock (Lock)
        {
            Console.WriteLine($"Total runs: {RunCount}");
            Console.WriteLine($"Unique TestIds: {SeenTestIds.Count}");
            Console.WriteLine($"TestIds:");
            foreach (var id in SeenTestIds)
            {
                Console.WriteLine($"  {id}");
            }

            // We should have 4 runs (repeat count of 3 means 0, 1, 2, 3)
            if (RunCount != 4)
            {
                throw new Exception($"Expected 4 test runs with Repeat(3), but got {RunCount}");
            }

            // We should have 4 unique test IDs
            if (SeenTestIds.Count != 4)
            {
                throw new Exception($"Expected 4 unique TestIds with Repeat(3), but got {SeenTestIds.Count}. RepeatIndex is not working correctly!");
            }
        }
    }
}
