using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

public class DiagnosticsTests
{
    [Test]
    public async Task Unused_Setups_Detected()
    {
        var mock = Mock.Of<ICalculator>();

        // Configure two setups but only exercise one
        mock.Add(1, 2).Returns(3);
        mock.Add(10, 20).Returns(30);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2); // only exercise the first setup

        var diag = Mock.GetDiagnostics(mock);

        await Assert.That(diag.TotalSetups).IsEqualTo(2);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(1);
        await Assert.That(diag.UnusedSetups).HasCount().EqualTo(1);
        await Assert.That(diag.UnusedSetups[0].MemberName).IsEqualTo("Add");
    }

    [Test]
    public async Task Unmatched_Calls_Detected()
    {
        var mock = Mock.Of<ICalculator>();

        // Configure setup only for specific args
        mock.Add(1, 2).Returns(3);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2); // matches setup
        _ = calc.Add(5, 5); // no setup match — unmatched

        var diag = Mock.GetDiagnostics(mock);

        await Assert.That(diag.UnmatchedCalls).HasCount().EqualTo(1);
        await Assert.That(diag.UnmatchedCalls[0].MemberName).IsEqualTo("Add");
    }

    [Test]
    public async Task All_Setups_Exercised()
    {
        var mock = Mock.Of<ICalculator>();

        mock.Add(Any(), Any()).Returns(42);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);

        var diag = Mock.GetDiagnostics(mock);

        await Assert.That(diag.TotalSetups).IsEqualTo(1);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(1);
        await Assert.That(diag.UnusedSetups).HasCount().EqualTo(0);
    }

    [Test]
    public async Task No_Calls_Means_All_Setups_Unused()
    {
        var mock = Mock.Of<ICalculator>();

        mock.Add(1, 2).Returns(3);
        mock.Add(3, 4).Returns(7);

        var diag = Mock.GetDiagnostics(mock);

        await Assert.That(diag.TotalSetups).IsEqualTo(2);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(0);
        await Assert.That(diag.UnusedSetups).HasCount().EqualTo(2);
    }

    [Test]
    public async Task Matcher_Descriptions_Populated()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Is<int>(x => x > 0)).Returns(1);

        var diag = Mock.GetDiagnostics(mock);

        await Assert.That(diag.UnusedSetups).HasCount().EqualTo(1);
        var setup = diag.UnusedSetups[0];
        await Assert.That(setup.MatcherDescriptions).HasCount().EqualTo(2);
        await Assert.That(setup.MatcherDescriptions[0]).Contains("Any");
        await Assert.That(setup.MatcherDescriptions[1]).Contains("predicate");
    }

    [Test]
    public async Task Reset_Clears_Diagnostics()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(3);

        ICalculator calc = mock.Object;
        _ = calc.Add(5, 5); // unmatched

        Mock.Reset(mock);

        var diag = Mock.GetDiagnostics(mock);

        await Assert.That(diag.TotalSetups).IsEqualTo(0);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(0);
        await Assert.That(diag.UnusedSetups).HasCount().EqualTo(0);
        await Assert.That(diag.UnmatchedCalls).HasCount().EqualTo(0);
    }

    [Test]
    public async Task Empty_Mock_Has_Clean_Diagnostics()
    {
        var mock = Mock.Of<ICalculator>();

        var diag = Mock.GetDiagnostics(mock);

        await Assert.That(diag.TotalSetups).IsEqualTo(0);
        await Assert.That(diag.ExercisedSetups).IsEqualTo(0);
        await Assert.That(diag.UnusedSetups).HasCount().EqualTo(0);
        await Assert.That(diag.UnmatchedCalls).HasCount().EqualTo(0);
    }
}
