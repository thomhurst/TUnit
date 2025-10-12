using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests2117
{
    [Test]
    [Arguments(new[] { 1, 2, 3 }, new[] { 3, 2, 1 }, CollectionOrdering.Matching,
        """
        Expected to be equivalent to [3, 2, 1]
        but collection item at index 0 does not match: expected 3, but was 1

        at Assert.That(a).IsEquivalentTo(b, CollectionOrdering.Matching)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 1, 2, 3, 4 }, CollectionOrdering.Any,
        """
        Expected to be equivalent to [1, 2, 3, 4]
        but collection has 3 items but expected 4

        at Assert.That(a).IsEquivalentTo(b, CollectionOrdering.Any)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 1, 2, 3, 4 }, null,
        """
        Expected to be equivalent to [1, 2, 3, 4]
        but collection has 3 items but expected 4

        at Assert.That(a).IsEquivalentTo(b)
        """)]
    public async Task IsEquivalent_Fail(int[] a, int[] b, CollectionOrdering? collectionOrdering, string expectedError)
    {
        await Assert.That(async () =>
                await (collectionOrdering is null
                    ? Assert.That(a).IsEquivalentTo(b)
                    : Assert.That(a).IsEquivalentTo(b, collectionOrdering.Value))
            ).Throws<AssertionException>()
            .WithMessage(expectedError);
    }

    [Test]
    [Arguments(new[] { 1, 2, 3 }, new[] { 3, 2, 1 }, CollectionOrdering.Any,
        """
        Expected to not be equivalent to [3, 2, 1]
        but collections are equivalent but should not be

        at Assert.That(a).IsNotEquivalentTo(b, CollectionOrdering.Any)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, CollectionOrdering.Matching,
        """
        Expected to not be equivalent to [1, 2, 3]
        but collections are equivalent but should not be

        at Assert.That(a).IsNotEquivalentTo(b, CollectionOrdering.Matching)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, null,
        """
        Expected to not be equivalent to [1, 2, 3]
        but collections are equivalent but should not be

        at Assert.That(a).IsNotEquivalentTo(b)
        """)]
    public async Task IsNotEquivalent_Fail(int[] a, int[] b, CollectionOrdering? collectionOrdering, string expectedError)
    {
        await Assert.That(async () =>
                await (collectionOrdering is null
                    ? Assert.That(a).IsNotEquivalentTo(b)
                    : Assert.That(a).IsNotEquivalentTo(b, collectionOrdering.Value))
            ).Throws<AssertionException>()
            .WithMessage(expectedError);
    }
}
