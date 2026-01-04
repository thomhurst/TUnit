#if NET5_0_OR_GREATER
namespace TUnit.Assertions.Tests;

public class AsyncEnumerableAssertionTests
{
    // IsEmpty tests
    [Test]
    public async Task Test_AsyncEnumerable_IsEmpty_Passes()
    {
        var empty = AsyncEmpty<int>();
        await Assert.That(empty).IsEmpty();
    }

    [Test]
    public async Task Test_AsyncEnumerable_IsEmpty_Fails()
    {
        var items = AsyncRange(1, 3);
        var action = async () => await Assert.That(items).IsEmpty();

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("async enumerable contains items");
    }

    // IsNotEmpty tests
    [Test]
    public async Task Test_AsyncEnumerable_IsNotEmpty_Passes()
    {
        var items = AsyncRange(1, 5);
        await Assert.That(items).IsNotEmpty();
    }

    [Test]
    public async Task Test_AsyncEnumerable_IsNotEmpty_Fails()
    {
        var empty = AsyncEmpty<int>();
        var action = async () => await Assert.That(empty).IsNotEmpty();

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("async enumerable was empty");
    }

    // HasCount tests
    [Test]
    public async Task Test_AsyncEnumerable_HasCount_Passes()
    {
        var items = AsyncRange(1, 5);
        await Assert.That(items).HasCount(5);
    }

    [Test]
    public async Task Test_AsyncEnumerable_HasCount_Fails()
    {
        var items = AsyncRange(1, 3);
        var action = async () => await Assert.That(items).HasCount(5);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("found 3 items");
    }

    // Contains tests
    [Test]
    public async Task Test_AsyncEnumerable_Contains_Passes()
    {
        var items = AsyncRange(1, 10);
        await Assert.That(items).Contains(5);
    }

    [Test]
    public async Task Test_AsyncEnumerable_Contains_Fails()
    {
        var items = AsyncRange(1, 5);
        var action = async () => await Assert.That(items).Contains(99);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("item 99 was not found");
    }

    // DoesNotContain tests
    [Test]
    public async Task Test_AsyncEnumerable_DoesNotContain_Passes()
    {
        var items = AsyncRange(1, 5);
        await Assert.That(items).DoesNotContain(99);
    }

    [Test]
    public async Task Test_AsyncEnumerable_DoesNotContain_Fails()
    {
        var items = AsyncRange(1, 10);
        var action = async () => await Assert.That(items).DoesNotContain(5);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("item 5 was found");
    }

    // All tests
    [Test]
    public async Task Test_AsyncEnumerable_All_Passes()
    {
        var items = AsyncRange(1, 5);
        await Assert.That(items).All(x => x > 0);
    }

    [Test]
    public async Task Test_AsyncEnumerable_All_Fails()
    {
        var items = AsyncRange(-2, 5); // -2, -1, 0, 1, 2
        var action = async () => await Assert.That(items).All(x => x > 0);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("did not satisfy the predicate");
    }

    // Any tests
    [Test]
    public async Task Test_AsyncEnumerable_Any_Passes()
    {
        var items = AsyncRange(1, 10);
        await Assert.That(items).Any(x => x > 8);
    }

    [Test]
    public async Task Test_AsyncEnumerable_Any_Fails()
    {
        var items = AsyncRange(1, 5);
        var action = async () => await Assert.That(items).Any(x => x > 100);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("no items satisfied the predicate");
    }

    // Chaining tests
    [Test]
    public async Task Test_AsyncEnumerable_And_Chaining_Passes()
    {
        var items = AsyncRange(1, 5);
        await Assert.That(items)
            .IsNotEmpty()
            .And.HasCount(5)
            .And.Contains(3);
    }

    [Test]
    public async Task Test_AsyncEnumerable_Or_Chaining_Passes()
    {
        var items = AsyncRange(1, 5);
        await Assert.That(items)
            .Contains(1)
            .Or.Contains(99); // First passes, so overall passes
    }

    // Null handling
    [Test]
    public async Task Test_AsyncEnumerable_Null_Fails()
    {
        IAsyncEnumerable<int>? items = null;
        var action = async () => await Assert.That(items!).IsEmpty();

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("was null");
    }

    // String async enumerable
    [Test]
    public async Task Test_AsyncEnumerable_String_Contains()
    {
        var items = AsyncFromArray(new[] { "hello", "world", "test" });
        await Assert.That(items).Contains("world");
    }

    [Test]
    public async Task Test_AsyncEnumerable_String_All()
    {
        var items = AsyncFromArray(new[] { "hello", "world", "test" });
        await Assert.That(items).All(s => s.Length > 0);
    }

    // Custom object tests
    [Test]
    public async Task Test_AsyncEnumerable_CustomObject_Contains()
    {
        var person1 = new TestPerson("Alice", 30);
        var person2 = new TestPerson("Bob", 25);
        var items = AsyncFromArray(new[] { person1, person2 });

        await Assert.That(items).Contains(person1);
    }

    [Test]
    public async Task Test_AsyncEnumerable_CustomObject_Any()
    {
        var items = AsyncFromArray(new[]
        {
            new TestPerson("Alice", 30),
            new TestPerson("Bob", 25),
            new TestPerson("Charlie", 35)
        });

        await Assert.That(items).Any(p => p.Age > 30);
    }

    // Helper methods for creating async enumerables
    private static async IAsyncEnumerable<T> AsyncEmpty<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<int> AsyncRange(int start, int count)
    {
        for (int i = start; i < start + count; i++)
        {
            await Task.Yield(); // Simulate async work
            yield return i;
        }
    }

    private static async IAsyncEnumerable<T> AsyncFromArray<T>(T[] items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private record TestPerson(string Name, int Age);
}
#endif
