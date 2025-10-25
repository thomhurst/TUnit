namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Tests for issue #3521: Assert.ThrowsAsync(Func&lt;Task&gt;) incorrectly passes when no exception is thrown
/// </summary>
public class Tests3521
{
    private static Task DoesNotThrow() => Task.CompletedTask;
    private static void DoesNotThrowSync() { }

    [Test]
    public async Task ThrowsAsync_WithMethodGroup_ShouldFailWhenNoExceptionThrown()
    {
        // This should fail because DoesNotThrow doesn't throw an exception
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.ThrowsAsync(DoesNotThrow));

        await TUnitAssert.That(exception.Message).Contains("but no exception was thrown");
    }

    [Test]
    public async Task ThrowsAsync_WithLambda_ShouldFailWhenNoExceptionThrown()
    {
        // This should fail because the lambda doesn't throw an exception
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.ThrowsAsync(() => DoesNotThrow()));

        await TUnitAssert.That(exception.Message).Contains("but no exception was thrown");
    }

    [Test]
    public async Task ThrowsAsync_WithAsyncLambda_ShouldFailWhenNoExceptionThrown()
    {
        // This should fail because the async lambda doesn't throw an exception
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.ThrowsAsync(async () => await DoesNotThrow()));

        await TUnitAssert.That(exception.Message).Contains("but no exception was thrown");
    }

    [Test]
    public async Task ThrowsAsync_Generic_WithMethodGroup_ShouldFailWhenNoExceptionThrown()
    {
        // Test the generic version with Exception type explicitly
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.ThrowsAsync<Exception>(DoesNotThrow));

        await TUnitAssert.That(exception.Message).Contains("but no exception was thrown");
    }

    [Test]
    public async Task ThrowsAsync_Type_ShouldFailWhenNoExceptionThrown()
    {
        // Test the Type-based overload
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.ThrowsAsync(typeof(Exception), DoesNotThrow));

        await TUnitAssert.That(exception.Message).Contains("but no exception was thrown");
    }

    [Test]
    public async Task Throws_Generic_ShouldFailWhenNoExceptionThrown()
    {
        // Test the synchronous version
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
        {
            TUnitAssert.Throws<Exception>(DoesNotThrowSync);
            await Task.CompletedTask;
        });

        await TUnitAssert.That(exception.Message).Contains("but no exception was thrown");
    }

    [Test]
    public async Task Throws_Type_ShouldFailWhenNoExceptionThrown()
    {
        // Test the synchronous Type-based overload
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
        {
            TUnitAssert.Throws(typeof(Exception), DoesNotThrowSync);
            await Task.CompletedTask;
        });

        await TUnitAssert.That(exception.Message).Contains("but no exception was thrown");
    }

    [Test]
    public async Task ThrowsAsync_ShouldStillWorkWhenExceptionIsThrown()
    {
        // Verify that ThrowsAsync still works correctly when an exception IS thrown
        var thrownException = new InvalidOperationException("Test exception");

        var caughtException = await TUnitAssert.ThrowsAsync(async () =>
        {
            await Task.Yield();
            throw thrownException;
        });

        await TUnitAssert.That(caughtException).IsSameReferenceAs(thrownException);
    }

    [Test]
    public async Task Throws_ShouldStillWorkWhenExceptionIsThrown()
    {
        // Verify that Throws still works correctly when an exception IS thrown
        var thrownException = new InvalidOperationException("Test exception");

        var caughtException = TUnitAssert.Throws<Exception>(() => throw thrownException);

        await TUnitAssert.That(caughtException).IsSameReferenceAs(thrownException);
    }
}
