using TUnit.Mocks.Arguments;
using TUnit.Mocks.Matchers;
using TUnit.Assertions.Enums;

namespace TUnit.Mocks.Tests;

public class CaptureStorageTests
{
    [Test]
    public async Task Snapshots_Stay_Stable_Across_Growth_And_Latest_Tracks_The_Last_Capture()
    {
        var arg = Arg.Any<int>();
        var mock = ICalculator.Mock();
        mock.Add(arg, Arg.Any<int>()).Returns(42);

        mock.Object.Add(0, 0);
        var firstSnapshot = arg.Values;
        for (var i = 1; i < 257; i++)
        {
            mock.Object.Add(i, 0);
        }

        await Assert.That(firstSnapshot).IsEquivalentTo(new[] { 0 });
        var values = arg.Values;
        await Assert.That(values.Count).IsEqualTo(257);
        for (var i = 0; i < values.Count; i++)
        {
            await Assert.That(values[i]).IsEqualTo(i);
        }
        await Assert.That(arg.Latest).IsEqualTo(256);
    }

    [Test]
    public async Task Latest_Does_Not_Allocate_After_Capture_History_Grows()
    {
        var arg = Arg.Any<int>();
        var mock = ICalculator.Mock();
        mock.Add(arg, Arg.Any<int>()).Returns(42);

        for (var i = 1; i <= 4096; i++)
        {
            mock.Object.Add(i, 0);
        }

        _ = arg.Latest;
        var before = GC.GetAllocatedBytesForCurrentThread();
        var latest = arg.Latest;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        await Assert.That(latest).IsEqualTo(4096);
        await Assert.That(allocated).IsEqualTo(0);
    }

    [Test]
    public async Task Shared_Arguments_Capture_Each_Position_And_Survive_Clear_And_Reset()
    {
        var arg = Arg.Any<int>();
        var first = ICalculator.Mock();
        var second = ICalculator.Mock();
        first.Add(arg, arg).Returns(1);
        second.Add(arg, Arg.Any<int>()).Returns(2);

        first.Object.Add(1, 2);
        second.Object.Add(3, 4);
        Mock.ClearCalls(first);
        Mock.Reset(second);
        first.Object.Add(5, 6);

        var values = arg.Values;
        await Assert.That(values.Count).IsEqualTo(5);
        await Assert.That(values[0]).IsEqualTo(1);
        await Assert.That(values[1]).IsEqualTo(2);
        await Assert.That(values[2]).IsEqualTo(3);
        await Assert.That(values[3]).IsEqualTo(5);
        await Assert.That(values[4]).IsEqualTo(6);
        await Assert.That(arg.Latest).IsEqualTo(6);
    }

    [Test]
    public async Task Typed_And_Untyped_Captures_Preserve_Null_Values()
    {
        var arg = Arg.Any<string?>();
        var typed = (ICapturingMatcher<string?>)arg.Matcher;
        var untyped = (ICapturingMatcher)arg.Matcher;

        typed.ApplyCapture("first");
        untyped.ApplyCapture(null);
        var snapshot = arg.Values;
        await Assert.That(arg.Latest).IsNull();
        typed.ApplyCapture("last");
        untyped.ApplyCapture(null);
        typed.ApplyCapture("promoted");

        await Assert.That(snapshot).IsEquivalentTo(new string?[] { "first", null });
        await Assert.That(arg.Values).IsEquivalentTo(new string?[] { "first", null, "last", null, "promoted" }, CollectionOrdering.Matching);
        await Assert.That(arg.Latest).IsEqualTo("promoted");
        untyped.ApplyCapture(null);
        await Assert.That(arg.Latest).IsNull();
    }

    [Test]
    public async Task Concurrent_Captures_Do_Not_Lose_Values_Or_Tear_Struct_Reads()
    {
        var matcher = new CapturingMatcher<WideValue>(AnyMatcher<WideValue>.Instance);
        var capture = (ICapturingMatcher<WideValue>)matcher;
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = 0;
        var reader = Task.Run(async () =>
        {
            await start.Task;
            while (Volatile.Read(ref stopped) == 0)
            {
                var latest = matcher.Latest;
                CheckValue(latest);
                var foundLatest = latest == default;
                foreach (var value in matcher.CapturedValues)
                {
                    CheckValue(value);
                    foundLatest |= value == latest;
                }

                if (!foundLatest)
                {
                    throw new InvalidOperationException("The capture snapshot omitted the latest value read before it.");
                }

                await Task.Yield();
            }
        });
        var writers = Enumerable.Range(0, 8).Select(worker => Task.Run(async () =>
        {
            await start.Task;
            for (var i = 1; i <= 512; i++)
            {
                var value = worker * 512 + i;
                capture.ApplyCapture(new WideValue(value, value, value, value));
            }
        })).ToArray();

        start.SetResult();
        try
        {
            await Task.WhenAll(writers);
        }
        finally
        {
            Volatile.Write(ref stopped, 1);
            await reader;
        }

        var values = matcher.CapturedValues;
        await Assert.That(values.Count).IsEqualTo(4096);
        await Assert.That(values.Select(value => value.A).Order()).IsEquivalentTo(Enumerable.Range(1, 4096).Select(i => (long)i));
        await Assert.That(matcher.Latest).IsEqualTo(values[^1]);
    }

    private static void CheckValue(WideValue value)
    {
        if (value.A != value.B || value.A != value.C || value.A != value.D)
        {
            throw new InvalidOperationException("Capture returned a torn struct value.");
        }
    }

    private readonly record struct WideValue(long A, long B, long C, long D);
}
