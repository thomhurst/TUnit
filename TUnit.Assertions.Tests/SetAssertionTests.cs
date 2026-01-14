using System.Collections.Immutable;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for set-specific assertion methods.
/// Covers ISet, IReadOnlySet, and HashSet types.
/// </summary>
public class SetAssertionTests
{
    // ===================================
    // IsSubsetOf Tests
    // ===================================

    [Test]
    public async Task HashSet_IsSubsetOf_Passes_When_Subset()
    {
        var subset = new HashSet<int> { 1, 2 };
        var superset = new HashSet<int> { 1, 2, 3, 4 };

        await Assert.That(subset).IsSubsetOf(superset);
    }

    [Test]
    public async Task HashSet_IsSubsetOf_Passes_When_Equal()
    {
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 1, 2, 3 };

        await Assert.That(set1).IsSubsetOf(set2);
    }

    [Test]
    public async Task HashSet_IsSubsetOf_Fails_When_Not_Subset()
    {
        var set = new HashSet<int> { 1, 2, 5 };
        var other = new HashSet<int> { 1, 2, 3 };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(set).IsSubsetOf(other));

        await Assert.That(exception.Message).Contains("subset");
    }

    [Test]
    public async Task ISet_IsSubsetOf_Works()
    {
        ISet<string> subset = new HashSet<string> { "a", "b" };
        var superset = new HashSet<string> { "a", "b", "c" };

        await Assert.That(subset).IsSubsetOf(superset);
    }

#if NET5_0_OR_GREATER
    [Test]
    public async Task IReadOnlySet_IsSubsetOf_Works()
    {
        IReadOnlySet<int> subset = new HashSet<int> { 1 };
        var superset = new HashSet<int> { 1, 2 };

        await Assert.That(subset).IsSubsetOf(superset);
    }
#endif

    // ===================================
    // IsSupersetOf Tests
    // ===================================

    [Test]
    public async Task HashSet_IsSupersetOf_Passes_When_Superset()
    {
        var superset = new HashSet<int> { 1, 2, 3, 4 };
        var subset = new HashSet<int> { 1, 2 };

        await Assert.That(superset).IsSupersetOf(subset);
    }

    [Test]
    public async Task HashSet_IsSupersetOf_Fails_When_Not_Superset()
    {
        var set = new HashSet<int> { 1, 2 };
        var other = new HashSet<int> { 1, 2, 3 };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(set).IsSupersetOf(other));

        await Assert.That(exception.Message).Contains("superset");
    }

    // ===================================
    // IsProperSubsetOf Tests
    // ===================================

    [Test]
    public async Task HashSet_IsProperSubsetOf_Passes_When_ProperSubset()
    {
        var subset = new HashSet<int> { 1, 2 };
        var superset = new HashSet<int> { 1, 2, 3 };

        await Assert.That(subset).IsProperSubsetOf(superset);
    }

    [Test]
    public async Task HashSet_IsProperSubsetOf_Fails_When_Equal()
    {
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 1, 2, 3 };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(set1).IsProperSubsetOf(set2));

        await Assert.That(exception.Message).Contains("proper subset");
    }

    // ===================================
    // IsProperSupersetOf Tests
    // ===================================

    [Test]
    public async Task HashSet_IsProperSupersetOf_Passes_When_ProperSuperset()
    {
        var superset = new HashSet<int> { 1, 2, 3, 4 };
        var subset = new HashSet<int> { 1, 2 };

        await Assert.That(superset).IsProperSupersetOf(subset);
    }

    [Test]
    public async Task HashSet_IsProperSupersetOf_Fails_When_Equal()
    {
        var set1 = new HashSet<int> { 1, 2 };
        var set2 = new HashSet<int> { 1, 2 };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(set1).IsProperSupersetOf(set2));

        await Assert.That(exception.Message).Contains("proper superset");
    }

    // ===================================
    // Overlaps Tests
    // ===================================

    [Test]
    public async Task HashSet_Overlaps_Passes_When_Has_Common_Elements()
    {
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 3, 4, 5 };

        await Assert.That(set1).Overlaps(set2);
    }

    [Test]
    public async Task HashSet_Overlaps_Fails_When_No_Common_Elements()
    {
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 4, 5, 6 };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(set1).Overlaps(set2));

        await Assert.That(exception.Message).Contains("overlap");
    }

    // ===================================
    // DoesNotOverlap Tests
    // ===================================

    [Test]
    public async Task HashSet_DoesNotOverlap_Passes_When_No_Common_Elements()
    {
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 4, 5, 6 };

        await Assert.That(set1).DoesNotOverlap(set2);
    }

    [Test]
    public async Task HashSet_DoesNotOverlap_Fails_When_Has_Common_Elements()
    {
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 3, 4, 5 };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(set1).DoesNotOverlap(set2));

        await Assert.That(exception.Message).Contains("not overlap");
    }

    // ===================================
    // SetEquals Tests
    // ===================================

    [Test]
    public async Task HashSet_SetEquals_Passes_When_Same_Elements()
    {
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 3, 1, 2 };  // Different order

        await Assert.That(set1).SetEquals(set2);
    }

    [Test]
    public async Task HashSet_SetEquals_Fails_When_Different_Elements()
    {
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 1, 2, 4 };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(set1).SetEquals(set2));

        await Assert.That(exception.Message).Contains("equal");
    }

    // ===================================
    // Chaining Tests
    // ===================================

    [Test]
    public async Task HashSet_And_Chain_Works()
    {
        var set = new HashSet<int> { 1, 2, 3 };
        var other = new HashSet<int> { 1, 2, 3, 4, 5 };

        await Assert.That(set)
            .IsSubsetOf(other)
            .And.IsNotEmpty()
            .And.HasCount(3);
    }

    [Test]
    public async Task HashSet_Or_Chain_Works()
    {
        var set = new HashSet<int> { 1, 2, 3 };
        var smallSet = new HashSet<int> { 1 };
        var largeSet = new HashSet<int> { 1, 2, 3, 4 };

        // One fails, one passes - should pass overall
        await Assert.That(set)
            .IsProperSubsetOf(smallSet)  // Fails
            .Or.IsSubsetOf(largeSet);    // Passes
    }

    [Test]
    public async Task HashSet_Inherits_Collection_Methods()
    {
        var set = new HashSet<int> { 1, 2, 3 };

        // Set should have access to collection methods
        await Assert.That(set)
            .IsNotEmpty()
            .And.HasCount(3)
            .And.Contains(2);
    }

    // ===================================
    // Null Handling Tests
    // ===================================

    [Test]
    public async Task HashSet_IsSubsetOf_Handles_Empty_Set()
    {
        var emptySet = new HashSet<int>();
        var anySet = new HashSet<int> { 1, 2 };

        // Empty set is subset of any set
        await Assert.That(emptySet).IsSubsetOf(anySet);
    }

    [Test]
    public async Task HashSet_IsSupersetOf_Handles_Empty_Other()
    {
        var set = new HashSet<int> { 1, 2 };
        var emptySet = new HashSet<int>();

        // Any set is superset of empty set
        await Assert.That(set).IsSupersetOf(emptySet);
    }

    // ===================================
    // String Set Tests
    // ===================================

    [Test]
    public async Task HashSet_String_Works()
    {
        var set = new HashSet<string> { "apple", "banana", "cherry" };
        var superset = new HashSet<string> { "apple", "banana", "cherry", "date" };

        await Assert.That(set).IsProperSubsetOf(superset);
    }

    [Test]
    public async Task Set_is_empty()
    {
        var set = ImmutableHashSet<int>.Empty;
        await Assert.That(set).IsEmpty();
    }
}
