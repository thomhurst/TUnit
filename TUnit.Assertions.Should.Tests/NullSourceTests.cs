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
}
