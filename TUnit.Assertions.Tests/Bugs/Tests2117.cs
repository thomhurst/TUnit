using System.Collections.Immutable;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests2117
{
    [Test]
    [Arguments(new[] { 1, 2, 3 }, new[] { 3, 2, 1 }, CollectionOrdering.Matching,
        """
        Expected a to be equivalent to [3, 2, 1]

        but it is [1, 2, 3]

        at Assert.That(a).IsEquivalentTo(b, collectionOrdering.Value)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 1, 2, 3, 4 }, CollectionOrdering.Any,
        """
        Expected a to be equivalent to [1, 2, 3, 4]

        but it is [1, 2, 3]

        at Assert.That(a).IsEquivalentTo(b, collectionOrdering.Value)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 1, 2, 3, 4 }, null,
        """
        Expected a to be equivalent to [1, 2, 3, 4]

        but it is [1, 2, 3]

        at Assert.That(a).IsEquivalentTo(b)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 3, 2, 1 }, null,
        """
        Expected a to be equivalent to [3, 2, 1]

        but it is [1, 2, 3]

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
        Expected a to not be equivalent to [3, 2, 1]

        but the two Enumerables were equivalent

        at Assert.That(a).IsNotEquivalentTo(b, collectionOrdering.Value)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, CollectionOrdering.Matching,
        """
        Expected a to not be equivalent to [1, 2, 3]

        but the two Enumerables were equivalent

        at Assert.That(a).IsNotEquivalentTo(b, collectionOrdering.Value)
        """)]
    [Arguments(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, null,
        """
        Expected a to not be equivalent to [1, 2, 3]

        but the two Enumerables were equivalent

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