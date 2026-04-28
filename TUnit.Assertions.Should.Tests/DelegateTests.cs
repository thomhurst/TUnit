using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

public class DelegateTests
{
    [Test]
    public async Task Action_Throw_subclass_passes()
    {
        Action act = () => throw new ArgumentNullException();
        await act.Should().Throw<ArgumentException>();
    }

    [Test]
    public async Task Action_ThrowExactly_subclass_fails()
    {
        Action act = () => throw new ArgumentNullException();
        await Assert.That(async () => await act.Should().ThrowExactly<ArgumentException>())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Action_ThrowExactly_exact_match_passes()
    {
        Action act = () => throw new InvalidOperationException();
        await act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Action_no_exception_Throw_fails()
    {
        Action act = () => { };
        await Assert.That(async () => await act.Should().Throw<InvalidOperationException>())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task FuncTask_Throw()
    {
        Func<Task> act = () => throw new InvalidOperationException("boom");
        await act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task FuncTaskT_NotThrow_returns_value()
    {
        Func<Task<int>> f = () => Task.FromResult(42);
        await f.Should().BeEqualTo(42);
    }

    [Test]
    public async Task FuncT_evaluates_only_once()
    {
        var count = 0;
        Func<int> f = () => { count++; return 42; };
        await f.Should().BeEqualTo(42).And.NotBeEqualTo(99);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Throw_failure_message_contains_method_name()
    {
        Action act = () => { };
        var ex = await Assert.That(async () => await act.Should().Throw<InvalidOperationException>())
            .Throws<AssertionException>();
        await Assert.That(ex.Message).Contains(".Should().Throw<");
    }
}
