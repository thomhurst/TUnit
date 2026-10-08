using FsCheck;
using Microsoft.FSharp.Collections;
using TUnit.FsCheck;

namespace TUnit.UnitTests;

public class FsCheckPropertyConfigTests
{
    [Test]
    public async Task Defaults_ReportSummary_RunSequentially_AndDoNotReplay()
    {
        var config = CreateConfig(new FsCheckPropertyAttribute());

        await Assert.That(config.QuietOnSuccess).IsFalse();
        await Assert.That(config.Every.Invoke(0).Invoke(FSharpList<object>.Empty)).IsEmpty();
        await Assert.That(config.ParallelRunConfig).IsNull();
        await Assert.That(config.Replay).IsNull();
    }

    [Test]
    public async Task QuietOnSuccess_IsPassedToFsCheck()
    {
        var config = CreateConfig(new FsCheckPropertyAttribute { QuietOnSuccess = true });

        await Assert.That(config.QuietOnSuccess).IsTrue();
    }

    [Test]
    public async Task Verbose_ReportsEveryArgumentAndShrinkStep()
    {
        var config = CreateConfig(new FsCheckPropertyAttribute { Verbose = true });

        await Assert.That(config.Every.Invoke(3).Invoke(FSharpList<object>.Empty)).StartsWith("3:");
        await Assert.That(config.EveryShrink.Invoke(FSharpList<object>.Empty)).StartsWith("shrink:");
    }

    [Test]
    public async Task Verbose_StillThrowsOnFailure()
    {
        // FsCheck's plain Verbose config only prints a failure; the test must still fail.
        var config = CreateConfig(new FsCheckPropertyAttribute { Verbose = true, MaxTest = 1 });

        await Assert.That(() => Check.One(config, false)).Throws<Exception>().WithMessageContaining("Falsifiable");
    }

    [Test]
    [Arguments(-1)]
    [Arguments(0)]
    [Arguments(1)]
    public async Task ParallelismBelowTwo_RunsSequentially(int parallelism)
    {
        var config = CreateConfig(new FsCheckPropertyAttribute { Parallelism = parallelism });

        await Assert.That(config.ParallelRunConfig).IsNull();
    }

    [Test]
    public async Task ParallelismAboveOne_IsPassedToFsCheck()
    {
        var config = CreateConfig(new FsCheckPropertyAttribute { Parallelism = 4 });

        await Assert.That(config.ParallelRunConfig).IsNotNull();
        await Assert.That(config.ParallelRunConfig.Value.MaxDegreeOfParallelism).IsEqualTo(4);
    }

    [Test]
    [Arguments("12345,67891", 12345UL, 67891UL, null)]
    [Arguments("(12345,67891)", 12345UL, 67891UL, null)]
    [Arguments(" ( 12345 , 67891 ) ", 12345UL, 67891UL, null)]
    [Arguments("(12345,67891,42)", 12345UL, 67891UL, 42)]
    [Arguments("18446744073709551615,18446744073709551615", ulong.MaxValue, ulong.MaxValue, null)]
    public async Task Replay_AcceptsValuesAsFsCheckReportsThem(string replay, ulong seed, ulong gamma, int? size)
    {
        var config = CreateConfig(new FsCheckPropertyAttribute { Replay = replay });

        await Assert.That(config.Replay).IsNotNull();
        await Assert.That(config.Replay.Value.Rnd.Seed).IsEqualTo(seed);
        await Assert.That(config.Replay.Value.Rnd.Gamma).IsEqualTo(gamma);
        await Assert.That(config.Replay.Value.Size is null ? (int?) null : config.Replay.Value.Size.Value).IsEqualTo(size);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Replay_BlankMeansNoReplay(string replay)
    {
        var config = CreateConfig(new FsCheckPropertyAttribute { Replay = replay });

        await Assert.That(config.Replay).IsNull();
    }

    [Test]
    [Arguments("12345")]
    [Arguments("12345,67890")]
    [Arguments("12345,67891,42,1")]
    [Arguments("12345,67891,-42")]
    [Arguments("12345,,67891")]
    [Arguments("-12345,67891")]
    [Arguments("abc,67891")]
    [Arguments("12345;67891")]
    [Arguments("(12345,67891")]
    [Arguments("12345,67891)")]
    [Arguments("((12345,67891))")]
    [Arguments("(12345,67891))")]
    public async Task Replay_RejectsValuesFsCheckCannotReplay(string replay)
    {
        var executor = new FsCheckPropertyTestExecutor(new FsCheckPropertyAttribute { Replay = replay });

        await Assert.That(() => executor.CreateConfig())
            .Throws<InvalidOperationException>()
            .WithMessageContaining($"Invalid Replay value '{replay}'");
    }

    private static Config CreateConfig(FsCheckPropertyAttribute attribute) =>
        new FsCheckPropertyTestExecutor(attribute).CreateConfig();
}
