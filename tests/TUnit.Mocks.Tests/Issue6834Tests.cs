using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6834
// Mocking one type both regularly and through Mock.Wrap produced two models differing only in
// IsWrapMock. Both emitted the same hint names, so the second AddSource threw and the generator
// aborted, taking every mock in the compilation with it. Each mode now emits its own impl and
// factory, and the member surface — which belongs to the type, not the mode — is emitted once.

#region Test types

public class DualModeCalculator
{
    private readonly int _offset;

    public DualModeCalculator() : this(0) { }

    public DualModeCalculator(int offset) => _offset = offset;

    public virtual int Add(int a, int b) => a + b + _offset;

    public virtual int Value { get; set; } = 7;

    public virtual event EventHandler<int>? Computed;

    public void RaiseComputed(int value) => Computed?.Invoke(this, value);
}

#endregion

public class Issue6834Tests
{
    [Test]
    public async Task Regular_Mock_Of_A_Type_That_Is_Also_Wrapped()
    {
        var mock = DualModeCalculator.Mock();
        mock.Add(1, 2).Returns(99);

        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(99);
        await Assert.That(mock.Object.Value).IsEqualTo(7);
    }

    [Test]
    public async Task Wrap_Mock_Of_A_Type_That_Is_Also_Mocked_Regularly()
    {
        var real = new DualModeCalculator(10);
        var mock = Mock.Wrap(real);

        // Unconfigured calls still reach the wrapped instance.
        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(13);

        mock.Add(1, 2).Returns(99);

        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(99);
    }

    [Test]
    public async Task Both_Modes_Share_One_Setup_Surface()
    {
        var regular = DualModeCalculator.Mock();
        var wrapped = Mock.Wrap(new DualModeCalculator(10));

        // The same extension members configure and verify either mock.
        regular.Value.Returns(1);
        wrapped.Value.Returns(2);

        await Assert.That(regular.Object.Value).IsEqualTo(1);
        await Assert.That(wrapped.Object.Value).IsEqualTo(2);

        regular.Add(Arg.Any<int>(), Arg.Any<int>()).WasNeverCalled();
        _ = wrapped.Object.Add(1, 2);
        wrapped.Add(1, 2).WasCalled();
    }

    [Test]
    public async Task Raise_Extensions_Are_Emitted_Once_And_Serve_Both_Modes()
    {
        var regular = DualModeCalculator.Mock();
        var wrapped = Mock.Wrap(new DualModeCalculator());

        var seen = new List<int>();
        regular.Object.Computed += (_, value) => seen.Add(value);
        wrapped.Object.Computed += (_, value) => seen.Add(value);

        regular.RaiseComputed(1);
        wrapped.RaiseComputed(2);

        await Assert.That(seen).IsEquivalentTo(new[] { 1, 2 });
    }
}
