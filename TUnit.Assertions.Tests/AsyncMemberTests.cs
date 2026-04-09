namespace TUnit.Assertions.Tests;

public class AsyncMemberTests
{
    private sealed class MyObject
    {
        public required string Name { get; init; }
        public required int Number { get; init; }

        public Task<string> ReadStringAsync() => Task.FromResult(Name);

        public Task<int> ReadNumberAsync() => Task.FromResult(Number);

        public async Task<string> ReadStringWithDelayAsync()
        {
            await Task.Delay(10);
            return Name;
        }

        public Task<string> ThrowingAsync() => Task.FromException<string>(new InvalidOperationException("Boom"));

        public Task<string?> ReadNullableStringAsync() => Task.FromResult<string?>(null);

        public Task<string?> ReadNullableStringWithValueAsync() => Task.FromResult<string?>(Name);
    }

    [Test]
    public async Task Async_Member_String_Success()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };
        await Assert.That(obj).Member(x => x.ReadStringAsync(), value => value.IsEqualTo("hello"));
    }

    [Test]
    public async Task Async_Member_Int_Success()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };
        await Assert.That(obj).Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(42));
    }

    [Test]
    public async Task Async_Member_With_Delay_Success()
    {
        var obj = new MyObject { Name = "delayed", Number = 1 };
        await Assert.That(obj).Member(x => x.ReadStringWithDelayAsync(), value => value.IsEqualTo("delayed"));
    }

    [Test]
    public async Task Async_Member_String_Contains()
    {
        var obj = new MyObject { Name = "hello world", Number = 1 };
        await Assert.That(obj).Member(x => x.ReadStringAsync(), value => value.Contains("world"));
    }

    [Test]
    public async Task Async_Member_Nullable_IsNull_Success()
    {
        var obj = new MyObject { Name = "test", Number = 1 };
        await Assert.That(obj).Member(x => x.ReadNullableStringAsync(), value => value.IsNull());
    }

    [Test]
    public async Task Async_Member_Nullable_IsNotNull_Success()
    {
        var obj = new MyObject { Name = "test", Number = 1 };
        await Assert.That(obj).Member(x => x.ReadNullableStringWithValueAsync(), value => value.IsNotNull());
    }

    [Test]
    public async Task Async_Member_Chained_With_And()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        await Assert.That(obj)
            .Member(x => x.ReadStringAsync(), value => value.IsEqualTo("hello"))
            .And.Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(42));
    }

    [Test]
    public async Task Async_Member_Chained_With_Sync_Member()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        await Assert.That(obj)
            .Member(x => x.Name, value => value.IsEqualTo("hello"))
            .And.Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(42));
    }

    [Test]
    public async Task Async_Member_Chained_Sync_After_Async()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        await Assert.That(obj)
            .Member(x => x.ReadStringAsync(), value => value.IsEqualTo("hello"))
            .And.Member(x => x.Number, value => value.IsEqualTo(42));
    }

    [Test]
    public async Task Async_Member_Chained_With_IsNotNull()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        await Assert.That(obj)
            .IsNotNull()
            .And.Member(x => x.ReadStringAsync(), value => value.IsEqualTo("hello"));
    }

    [Test]
    public async Task Async_Member_Chained_With_Or()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        await Assert.That(obj)
            .Member(x => x.ReadStringAsync(), value => value.IsEqualTo("wrong"))
            .Or.Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(42));
    }

    [Test]
    public async Task Async_Member_Multiple_Async_Chained()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        await Assert.That(obj)
            .Member(x => x.ReadStringAsync(), value => value.IsEqualTo("hello"))
            .And.Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(42))
            .And.Member(x => x.ReadStringWithDelayAsync(), value => value.IsEqualTo("hello"));
    }

    [Test]
    public async Task Async_Member_String_Failure()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(obj).Member(x => x.ReadStringAsync(), value => value.IsEqualTo("world")));

        await Assert.That(exception!.Message).Contains("world");
    }

    [Test]
    public async Task Async_Member_Int_Failure()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(obj).Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(99)));

        await Assert.That(exception!.Message).Contains("99");
        await Assert.That(exception.Message).Contains("42");
    }

    [Test]
    public async Task Async_Member_Throwing_Method()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(obj).Member(x => x.ThrowingAsync(), value => value.IsEqualTo("anything")));
    }

    [Test]
    public async Task Async_Member_Null_Object()
    {
        MyObject obj = null!;

        await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(obj).Member(x => x.ReadStringAsync(), value => value.IsEqualTo("hello")));
    }

    [Test]
    public async Task Async_Member_Chained_First_Fails()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(obj)
                .Member(x => x.ReadStringAsync(), value => value.IsEqualTo("wrong"))
                .And.Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(42)));

        await Assert.That(exception!.Message).Contains("wrong");
    }

    [Test]
    public async Task Async_Member_Chained_Second_Fails()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(obj)
                .Member(x => x.ReadStringAsync(), value => value.IsEqualTo("hello"))
                .And.Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(99)));

        await Assert.That(exception!.Message).Contains("99");
    }

    [Test]
    public async Task Async_Member_Or_Both_Fail()
    {
        var obj = new MyObject { Name = "hello", Number = 42 };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(obj)
                .Member(x => x.ReadStringAsync(), value => value.IsEqualTo("wrong"))
                .Or.Member(x => x.ReadNumberAsync(), value => value.IsEqualTo(99)));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Async_Member_Nullable_NotNull_Fails_When_Null()
    {
        var obj = new MyObject { Name = "test", Number = 1 };

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(obj).Member(x => x.ReadNullableStringAsync(), value => value.IsNotNull()));

        await Assert.That(exception!.Message).Contains("not be null");
    }
}
