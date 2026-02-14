using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class KeyAwareFixture : IKeyedDataSource, IAsyncInitializer
{
    public string Key { get; set; } = string.Empty;

    public string KeyDuringInit { get; private set; } = string.Empty;

    public Task InitializeAsync()
    {
        KeyDuringInit = Key;
        return Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Pass)]
[UnconditionalSuppressMessage("Usage", "TUnit0018:Test methods should not assign instance data")]
public class KeyedDataSourceTests
{
    private static readonly List<KeyAwareFixture> AlphaInstances = [];

    [Test]
    [ClassDataSource<KeyAwareFixture>(Shared = SharedType.Keyed, Key = "alpha")]
    public async Task Key_IsSetToAlpha(KeyAwareFixture fixture)
    {
        AlphaInstances.Add(fixture);
        await Assert.That(fixture.Key).IsEqualTo("alpha");
    }

    [Test]
    [ClassDataSource<KeyAwareFixture>(Shared = SharedType.Keyed, Key = "beta")]
    public async Task Key_IsSetToBeta(KeyAwareFixture fixture)
    {
        await Assert.That(fixture.Key).IsEqualTo("beta");
    }

    [Test]
    [ClassDataSource<KeyAwareFixture>(Shared = SharedType.Keyed, Key = "alpha")]
    public async Task Key_IsAvailableDuringInitializeAsync(KeyAwareFixture fixture)
    {
        AlphaInstances.Add(fixture);
        await Assert.That(fixture.KeyDuringInit).IsEqualTo("alpha");
    }

    [Test]
    [DependsOn(nameof(Key_IsSetToAlpha))]
    [DependsOn(nameof(Key_IsAvailableDuringInitializeAsync))]
    public async Task SameKey_ReturnsSameInstance()
    {
        await Assert.That(AlphaInstances).HasCount().EqualTo(2);
        await Assert.That(AlphaInstances[0]).IsSameReferenceAs(AlphaInstances[1]);
    }
}
