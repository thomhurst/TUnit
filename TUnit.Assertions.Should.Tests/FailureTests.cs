using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

/// <summary>
/// Focused failure-path coverage for the Should surface. These tests intentionally overlap
/// the passing-path feature tests so a broken Should adapter cannot accidentally keep only
/// happy-path coverage green.
/// </summary>
public class FailureTests
{
    [Test]
    public async Task Scalar_generated_extension_failure_throws_assertion_exception()
    {
        await Assert.That(async () => await 42.Should().BeEqualTo(7))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task String_generated_extension_failure_throws_assertion_exception()
    {
        await Assert.That(async () => await "hello".Should().Contain("missing"))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Collection_generated_extension_failure_throws_assertion_exception()
    {
        var values = new[] { 1, 2, 3 };
        await Assert.That(async () => await values.Should().Contain(99))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Collection_instance_method_failure_throws_assertion_exception()
    {
        var values = new[] { 3, 2, 1 };
        await Assert.That(async () => await values.Should().BeInOrder())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Collection_predicate_instance_method_failure_throws_assertion_exception()
    {
        var values = new[] { 1, 2, 3 };
        await Assert.That(async () => await values.Should().All(x => x > 1))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Delegate_assertion_failure_throws_assertion_exception()
    {
        Action action = () => { };
        await Assert.That(async () => await action.Should().Throw<InvalidOperationException>())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task GenerateAssertion_backed_failure_throws_assertion_exception()
    {
        await Assert.That(async () => await 5.Should().BeZero())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task User_defined_assertion_failure_throws_assertion_exception()
    {
        await Assert.That(async () => await 2.Should().BeOdd())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Chained_assertion_failure_throws_assertion_exception()
    {
        await Assert.That(async () => await 5.Should().BeEqualTo(5).And.BeEqualTo(99))
            .Throws<AssertionException>();
    }
}
