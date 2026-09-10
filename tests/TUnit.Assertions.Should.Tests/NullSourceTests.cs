using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

/// <summary>
/// Verifies null sources produce a meaningful assertion failure rather than an NRE.
/// </summary>
public class NullSourceTests
{
    [Test]
    public async Task Null_string_BeEqualTo_fails_with_assertion_exception()
    {
        string? value = null;
        await Assert.That(async () => await value.Should().BeEqualTo("expected"))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_collection_Contain_fails_with_assertion_exception()
    {
        List<int>? list = null;
        await Assert.That(async () => await list.Should().Contain(1))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_collection_BeInOrder_fails_with_assertion_exception()
    {
        List<int>? list = null;
        await Assert.That(async () => await list.Should().BeInOrder())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_collection_All_predicate_fails_with_assertion_exception()
    {
        List<int>? list = null;
        await Assert.That(async () => await list.Should().All(x => x > 0))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_collection_Any_predicate_fails_with_assertion_exception()
    {
        List<int>? list = null;
        await Assert.That(async () => await list.Should().Any(x => x > 0))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_collection_HaveSingleItem_predicate_fails_with_assertion_exception()
    {
        List<int>? list = null;
        await Assert.That(async () => await list.Should().HaveSingleItem(x => x > 0))
            .Throws<AssertionException>();
    }
}
