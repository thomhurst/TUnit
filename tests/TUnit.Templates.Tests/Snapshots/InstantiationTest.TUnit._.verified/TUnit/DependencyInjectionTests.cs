namespace TUnit;

public class DependencyInjectionTests
{
    // A new InMemoryDb is created for each test
    [Test]
    [ClassDataSource<InMemoryDb>]
    public async Task Database_SetAndGet(InMemoryDb db)
    {
        await db.SetAsync("key", "value");

        var result = await db.GetAsync("key");

        await Assert.That(result).IsEqualTo("value");
    }

    // The same InMemoryDb instance is shared across all tests in this class
    [Test]
    [ClassDataSource<InMemoryDb>(Shared = SharedType.PerClass)]
    public async Task Database_SharedPerClass(InMemoryDb db)
    {
        await db.SetAsync("shared", "data");

        var result = await db.GetAsync("shared");

        await Assert.That(result).IsNotNull();
    }
}

// ClassDataSource can also be applied at the class level
[ClassDataSource<InMemoryDb>(Shared = SharedType.PerTestSession)]
public class SharedDatabaseTests(InMemoryDb db)
{
    // Or injected as a property instead of a constructor parameter
    [ClassDataSource<Calculator>]
    public required Calculator Calculator { get; init; }

    [Test]
    public async Task Calculator_ResultCanBeStored()
    {
        var result = Calculator.Add(2, 3);

        await db.SetAsync("result", result.ToString());

        await Assert.That(await db.GetAsync("result")).IsEqualTo("5");
    }
}
