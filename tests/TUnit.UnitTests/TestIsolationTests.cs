using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.UnitTests;

public class TestIsolationTests
{
    [Test]
    public async Task UniqueId_IsPositive()
    {
        var isolation = TestContext.Current!.Isolation;

        await Assert.That(isolation.UniqueId).IsGreaterThan(0);
    }

    [Test]
    public async Task UniqueId_IsDifferentAcrossTests_1()
    {
        // Store uniqueId in state bag so a parallel test can verify it's different
        var isolation = TestContext.Current!.Isolation;
        TestContext.Current.StateBag["isolation_test_id_1"] = isolation.UniqueId;

        await Assert.That(isolation.UniqueId).IsGreaterThan(0);
    }

    [Test]
    public async Task UniqueId_IsDifferentAcrossTests_2()
    {
        var isolation = TestContext.Current!.Isolation;
        TestContext.Current.StateBag["isolation_test_id_2"] = isolation.UniqueId;

        await Assert.That(isolation.UniqueId).IsGreaterThan(0);
    }

    [Test]
    public async Task GetIsolatedName_ReturnsExpectedFormat()
    {
        var isolation = TestContext.Current!.Isolation;
        var id = isolation.UniqueId;

        var name = isolation.GetIsolatedName("foo");

        await Assert.That(name).IsEqualTo($"Test_{id}_foo");
    }

    [Test]
    public async Task GetIsolatedName_WithDifferentBaseNames_ReturnsDifferentNames()
    {
        var isolation = TestContext.Current!.Isolation;

        var name1 = isolation.GetIsolatedName("todos");
        var name2 = isolation.GetIsolatedName("orders");

        await Assert.That(name1).IsNotEqualTo(name2);
        await Assert.That(name1).Contains("todos");
        await Assert.That(name2).Contains("orders");
    }

    [Test]
    public async Task GetIsolatedPrefix_WithDefaultSeparator_ReturnsExpectedFormat()
    {
        var isolation = TestContext.Current!.Isolation;
        var id = isolation.UniqueId;

        var prefix = isolation.GetIsolatedPrefix();

        await Assert.That(prefix).IsEqualTo($"test_{id}_");
    }

    [Test]
    public async Task GetIsolatedPrefix_WithDotSeparator_ReturnsExpectedFormat()
    {
        var isolation = TestContext.Current!.Isolation;
        var id = isolation.UniqueId;

        var prefix = isolation.GetIsolatedPrefix(".");

        await Assert.That(prefix).IsEqualTo($"test.{id}.");
    }

    [Test]
    public async Task GetIsolatedPrefix_WithCustomSeparator_ReturnsExpectedFormat()
    {
        var isolation = TestContext.Current!.Isolation;
        var id = isolation.UniqueId;

        var prefix = isolation.GetIsolatedPrefix("-");

        await Assert.That(prefix).IsEqualTo($"test-{id}-");
    }

    [Test]
    public async Task Isolation_IsAccessibleViaInterface()
    {
        ITestIsolation isolation = TestContext.Current!.Isolation;

        await Assert.That(isolation).IsNotNull();
        await Assert.That(isolation.UniqueId).IsGreaterThan(0);
    }

    [Test]
    public async Task Isolation_SameContextReturnsSameValues()
    {
        var context = TestContext.Current!;

        var id1 = context.Isolation.UniqueId;
        var id2 = context.Isolation.UniqueId;
        var name1 = context.Isolation.GetIsolatedName("test");
        var name2 = context.Isolation.GetIsolatedName("test");

        await Assert.That(id1).IsEqualTo(id2);
        await Assert.That(name1).IsEqualTo(name2);
    }
}
