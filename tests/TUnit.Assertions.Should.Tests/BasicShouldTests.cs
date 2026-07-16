using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

public class BasicShouldTests
{
    [Test]
    public async Task Value_BeEqualTo_passes()
    {
        await 42.Should().BeEqualTo(42);
    }

    [Test]
    public async Task Value_NotBeEqualTo_passes()
    {
        await 42.Should().NotBeEqualTo(7);
    }

    [Test]
    public async Task Value_BeEqualTo_fails_with_message()
    {
        var ex = await Assert.That(async () => await 42.Should().BeEqualTo(7))
            .Throws<TUnit.Assertions.Exceptions.AssertionException>();
        await Assert.That(ex.Message).Contains("BeEqualTo");
    }

    [Test]
    public async Task String_BeEqualTo_works()
    {
        await "hello".Should().BeEqualTo("hello");
    }

    [Test]
    public async Task String_Contain_works()
    {
        await "hello world".Should().Contain("world");
    }

    [Test]
    public async Task String_StartWith_works()
    {
        await "hello world".Should().StartWith("hello");
    }

    [Test]
    public async Task Collection_Contain_works()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().Contain(2);
    }

    [Test]
    public async Task Chain_And_works()
    {
        await 42.Should().BeEqualTo(42).And.NotBeEqualTo(7);
    }

    [Test]
    public async Task Chain_Or_works()
    {
        await 42.Should().BeEqualTo(7).Or.BeEqualTo(42);
    }

    [Test]
    public async Task Default_BeDefault_works()
    {
        int value = 0;
        await value.Should().BeDefault();
    }

    [Test]
    public async Task NotDefault_NotBeDefault_works()
    {
        int value = 42;
        await value.Should().NotBeDefault();
    }

    [Test]
    public async Task Action_Throw_works()
    {
        Action act = () => throw new InvalidOperationException("boom");
        await act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task Action_ThrowExactly_works()
    {
        Action act = () => throw new InvalidOperationException("boom");
        await act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Test]
    public async Task FuncTask_Throw_works()
    {
        Func<Task> act = () => throw new InvalidOperationException("boom");
        await act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task FuncResult_does_not_throw()
    {
        Func<int> f = () => 42;
        await f.Should().NotBeEqualTo(7);
    }

    [Test]
    public async Task List_Contain_infers_element_type_without_cast()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().Contain(2);
    }

    [Test]
    public async Task ReadOnlyList_Contain_infers_element_type_without_cast()
    {
        IReadOnlyList<string> list = new[] { "a", "b", "c" };
        await list.Should().Contain("b");
    }

    [Test]
    public async Task Array_Contain_infers_element_type_without_cast()
    {
        var arr = new[] { 1, 2, 3 };
        await arr.Should().Contain(2);
    }
}
