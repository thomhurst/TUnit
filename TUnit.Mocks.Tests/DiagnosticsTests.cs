using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

public class DiagnosticsTests
{
    [Test]
    public async Task Unused_Setups_Detected()
    {
        var mock = ICalculator.Mock();

        // Configure two setups but only exercise one
        mock.Add(1, 2).Returns(3);
        mock.Add(10, 20).Returns(30);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2); // only exercise the first setup

        var diag = mock.GetDiagnostics();

        await Assert.That(diag.TotalSetups).IsEqualTo(2);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(1);
        await Assert.That(diag.UnusedSetups).Count().IsEqualTo(1);
        await Assert.That(diag.UnusedSetups[0].MemberName).IsEqualTo("Add");
    }

    [Test]
    public async Task Unmatched_Calls_Detected()
    {
        var mock = ICalculator.Mock();

        // Configure setup only for specific args
        mock.Add(1, 2).Returns(3);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2); // matches setup
        _ = calc.Add(5, 5); // no setup match — unmatched

        var diag = mock.GetDiagnostics();

        await Assert.That(diag.UnmatchedCalls).Count().IsEqualTo(1);
        await Assert.That(diag.UnmatchedCalls[0].MemberName).IsEqualTo("Add");
    }

    [Test]
    public async Task All_Setups_Exercised()
    {
        var mock = ICalculator.Mock();

        mock.Add(Any(), Any()).Returns(42);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);

        var diag = mock.GetDiagnostics();

        await Assert.That(diag.TotalSetups).IsEqualTo(1);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(1);
        await Assert.That(diag.UnusedSetups).Count().IsEqualTo(0);
    }

    [Test]
    public async Task No_Calls_Means_All_Setups_Unused()
    {
        var mock = ICalculator.Mock();

        mock.Add(1, 2).Returns(3);
        mock.Add(3, 4).Returns(7);

        var diag = mock.GetDiagnostics();

        await Assert.That(diag.TotalSetups).IsEqualTo(2);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(0);
        await Assert.That(diag.UnusedSetups).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Matcher_Descriptions_Populated()
    {
        var mock = ICalculator.Mock();
        mock.Add(Any(), Is<int>(x => x > 0)).Returns(1);

        var diag = mock.GetDiagnostics();

        await Assert.That(diag.UnusedSetups).Count().IsEqualTo(1);
        var setup = diag.UnusedSetups[0];
        await Assert.That(setup.MatcherDescriptions).Count().IsEqualTo(2);
        await Assert.That(setup.MatcherDescriptions[0]).Contains("Any");
        await Assert.That(setup.MatcherDescriptions[1]).Contains("predicate");
    }

    [Test]
    public async Task Reset_Clears_Diagnostics()
    {
        var mock = ICalculator.Mock();
        mock.Add(1, 2).Returns(3);

        ICalculator calc = mock.Object;
        _ = calc.Add(5, 5); // unmatched

        mock.Reset();

        var diag = mock.GetDiagnostics();

        await Assert.That(diag.TotalSetups).IsEqualTo(0);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(0);
        await Assert.That(diag.UnusedSetups).Count().IsEqualTo(0);
        await Assert.That(diag.UnmatchedCalls).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Empty_Mock_Has_Clean_Diagnostics()
    {
        var mock = ICalculator.Mock();

        var diag = mock.GetDiagnostics();

        await Assert.That(diag.TotalSetups).IsEqualTo(0);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(0);
        await Assert.That(diag.UnusedSetups).Count().IsEqualTo(0);
        await Assert.That(diag.UnmatchedCalls).Count().IsEqualTo(0);
    }
}
