using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

[NotInParallel]
public class MockSettingsTests
{
    private MockBehavior _savedDefaultMode;

    [Before(HookType.Test)]
    public void SnapshotSettings()
    {
        _savedDefaultMode = TUnitMocksSettings.Default.DefaultMode;
    }

    [After(HookType.Test)]
    public void RestoreSettings()
    {
        TUnitMocksSettings.Default.DefaultMode = _savedDefaultMode;
    }

    [Test]
    public async Task Default_Mode_Is_Loose()
    {
        await Assert.That(TUnitMocksSettings.Default.DefaultMode).IsEqualTo(MockBehavior.Loose);
    }

    [Test]
    public async Task Default_Mode_Applies_To_Parameterless_Mock_Factories()
    {
        TUnitMocksSettings.Default.DefaultMode = MockBehavior.Strict;

        var extensionMock = ICalculator.Mock();
        var staticMock = Mock.Of<IGreeter>();
        var repositoryMock = new MockRepository().Of<IRepoService>();

        await Assert.That(Mock.Behavior(extensionMock)).IsEqualTo(MockBehavior.Strict);
        await Assert.That(Mock.Behavior(staticMock)).IsEqualTo(MockBehavior.Strict);
        await Assert.That(Mock.Behavior(repositoryMock)).IsEqualTo(MockBehavior.Strict);
    }

    [Test]
    public async Task Explicit_Behavior_Overrides_Default_Mode()
    {
        TUnitMocksSettings.Default.DefaultMode = MockBehavior.Strict;

        var mock = ICalculator.Mock(MockBehavior.Loose);

        mock.Object.Add(1, 2);

        await Assert.That(Mock.Behavior(mock)).IsEqualTo(MockBehavior.Loose);
    }

    [Test]
    public async Task Strict_Default_Mode_Throws_For_Unconfigured_Calls()
    {
        TUnitMocksSettings.Default.DefaultMode = MockBehavior.Strict;

        var mock = ICalculator.Mock();

        var exception = Assert.Throws<MockStrictBehaviorException>(() => mock.Object.Add(1, 2));

        await Assert.That(exception.UnconfiguredCall).Contains("Add");
    }

    [Test]
    public async Task Repository_Default_Mode_Is_Resolved_When_Mock_Is_Created()
    {
        var repository = new MockRepository();

        TUnitMocksSettings.Default.DefaultMode = MockBehavior.Strict;

        var mock = repository.Of<IRepoService>();

        await Assert.That(Mock.Behavior(mock)).IsEqualTo(MockBehavior.Strict);
    }
}
