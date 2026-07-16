namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Tests for IsEquivalentTo with records containing Type properties.
/// Type objects have properties (like DeclaringMethod) that throw InvalidOperationException
/// when accessed on non-generic-parameter types, so Type must be treated as a well-known
/// primitive type and compared by equality rather than structural decomposition.
/// </summary>
public class IsEquivalentTo_TypeProperty_Tests
{
    public record RecordWithType(Type Type);

    public record RecordWithMultipleTypes(Type First, Type Second, string Name);

    [Test]
    public async Task IsEquivalentTo_RecordWithTypeProperty_SameType_ShouldSucceed()
    {
        var r = new RecordWithType(typeof(string));
        await Assert.That(r).IsEquivalentTo(r);
    }

    [Test]
    public async Task IsEquivalentTo_RecordWithTypeProperty_EqualTypes_ShouldSucceed()
    {
        var r1 = new RecordWithType(typeof(string));
        var r2 = new RecordWithType(typeof(string));
        await Assert.That(r1).IsEquivalentTo(r2);
    }

    [Test]
    public async Task IsEquivalentTo_RecordWithTypeProperty_DifferentTypes_ShouldFail()
    {
        var r1 = new RecordWithType(typeof(string));
        var r2 = new RecordWithType(typeof(int));

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(r1).IsEquivalentTo(r2));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IsEquivalentTo_RecordWithMultipleTypeProperties_ShouldSucceed()
    {
        var r1 = new RecordWithMultipleTypes(typeof(string), typeof(int), "test");
        var r2 = new RecordWithMultipleTypes(typeof(string), typeof(int), "test");
        await Assert.That(r1).IsEquivalentTo(r2);
    }

    [Test]
    public async Task IsEquivalentTo_TypeDirectly_ShouldSucceed()
    {
        Type t1 = typeof(string);
        Type t2 = typeof(string);
        await Assert.That(t1).IsEquivalentTo(t2);
    }
}
