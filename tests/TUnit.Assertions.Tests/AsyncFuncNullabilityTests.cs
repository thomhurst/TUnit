namespace TUnit.Assertions.Tests;

/// <summary>
/// A lambda that returns a <see cref="Task{TResult}"/> of a non-nullable reference type, passed to
/// <c>Assert.That</c> without <c>async</c>, binds without a nullability warning (CS8619) and asserts on the result.
/// </summary>
public class AsyncFuncNullabilityTests
{
    private static Task<string> GetNameAsync() => Task.FromResult("Alice");

    private static Task<string?> GetMissingNameAsync() => Task.FromResult<string?>(null);

    private static Task<int> GetCountAsync() => Task.FromResult(3);

    [Test]
    public async Task Non_Nullable_Reference_Result()
    {
        await Assert.That(() => GetNameAsync()).IsEqualTo("Alice");
    }

    [Test]
    public async Task Nullable_Reference_Result()
    {
        await Assert.That(() => GetMissingNameAsync()).IsNull();
    }

    [Test]
    public async Task Value_Type_Result()
    {
        await Assert.That(() => GetCountAsync()).IsEqualTo(3);
    }

    [Test]
    public async Task Awaiting_Gives_The_Result()
    {
        var name = await Assert.That(() => GetNameAsync()).IsNotNull();

        await Assert.That(name.Length).IsEqualTo(5);
    }

    [Test]
    public async Task Failure_Is_Reported()
    {
        var action = async () => await Assert.That(() => GetNameAsync()).IsEqualTo("Bob");

        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("Bob");
    }
}
