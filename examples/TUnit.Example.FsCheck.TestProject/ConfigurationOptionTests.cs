using TUnit.Core;
using TUnit.FsCheck;

namespace TUnit.Example.FsCheck.TestProject;

// Each class below checks FsCheck's console output (or other observed behaviour) in an [After(Test)]
// hook, i.e. once FsCheck has finished.

/// <summary>
/// Baseline for <see cref="QuietOnSuccessTests"/> and <see cref="VerboseTests"/>: without options
/// FsCheck reports the summary, but not the generated arguments.
/// </summary>
public sealed class DefaultOutputTests
{
    [Test, FsCheckProperty(MaxTest = 5)]
    public bool PassingProperty(int value) => true;

    [After(Test)]
    public async Task OutputHasSummaryButNoArguments(TestContext context)
    {
        var output = await CapturedOutput.ReadAsync(context);

        await Assert.That(output).Contains("Ok, passed 5 tests.");
        await Assert.That(output).DoesNotContain($"0:{Environment.NewLine}");
    }
}

public sealed class QuietOnSuccessTests
{
    [Test, FsCheckProperty(MaxTest = 5, QuietOnSuccess = true)]
    public bool PassingProperty(int value) => true;

    [After(Test)]
    public async Task OutputHasNoSummary(TestContext context)
    {
        await Assert.That(await CapturedOutput.ReadAsync(context)).DoesNotContain("Ok, passed");
    }
}

public sealed class VerboseTests
{
    [Test, FsCheckProperty(MaxTest = 5, Verbose = true)]
    public bool PassingProperty(int value) => true;

    [After(Test)]
    public async Task OutputHasEveryGeneratedArgument(TestContext context)
    {
        var output = await CapturedOutput.ReadAsync(context);

        // FsCheck's verbose format: "<test number>:" on its own line, followed by the arguments.
        for (var testNumber = 0; testNumber < 5; testNumber++)
        {
            await Assert.That(output).Contains($"{testNumber}:{Environment.NewLine}");
        }
    }
}

public sealed class ParallelismTests : IDisposable
{
    private readonly CountdownEvent _firstTwoInvocationsStarted = new(2);
    private int _invocations;
    private int _timeouts;

    /// <summary>
    /// The first two invocations wait for each other. Run sequentially, the first one can never
    /// see the second start, so it times out.
    /// </summary>
    [Test, FsCheckProperty(MaxTest = 20, Parallelism = 4)]
    public bool InvocationsOverlap(int value)
    {
        if (Interlocked.Increment(ref _invocations) <= 2)
        {
            _firstTwoInvocationsStarted.Signal();

            if (!_firstTwoInvocationsStarted.Wait(TimeSpan.FromSeconds(10)))
            {
                Interlocked.Increment(ref _timeouts);
            }
        }

        return true;
    }

    [After(Test)]
    public async Task FirstTwoInvocationsRanConcurrently()
    {
        await Assert.That(_invocations).IsEqualTo(20);
        await Assert.That(_timeouts).IsEqualTo(0);
    }

    public void Dispose() => _firstTwoInvocationsStarted.Dispose();
}

public sealed class ReplayTests
{
    private int _invocations;

    /// <summary>
    /// Value in the exact form of FsCheck's "Replay directly at failing step with (seed,gamma,size)"
    /// hint. With a size, FsCheck replays only that single step instead of a whole run.
    /// </summary>
    [Test, FsCheckProperty(Replay = "(12345,67891,5)")]
    public bool ReplayWithSizeRunsOnlyThatStep(int value)
    {
        Interlocked.Increment(ref _invocations);
        return true;
    }

    [After(Test)]
    public async Task PropertyRanOnce()
    {
        await Assert.That(_invocations).IsEqualTo(1);
    }
}

internal static class CapturedOutput
{
    /// <summary>
    /// FsCheck writes with <c>printf</c>, i.e. <see cref="TextWriter.Write(string)"/> without a
    /// <c>WriteLine</c>. TUnit buffers such partial lines per test and routes them to the test output
    /// on flush, which otherwise only happens after the <c>[After(Test)]</c> hooks.
    /// </summary>
    public static async Task<string> ReadAsync(TestContext context)
    {
        await Console.Out.FlushAsync();
        return context.GetStandardOutput();
    }
}
