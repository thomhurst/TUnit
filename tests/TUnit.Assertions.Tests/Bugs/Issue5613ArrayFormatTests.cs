using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #5613 finding #2: array IsEqualTo uses reference
/// equality, and failure messages rendered both sides as "System.UInt32[]" because
/// arrays don't override ToString. The message must:
///  - show element contents when references (and contents) differ, and
///  - disclose reference-equality semantics (especially when contents match), so users
///    see why a "visually-equal" assertion failed and are pointed at IsEquivalentTo.
/// </summary>
public class Issue5613ArrayFormatTests
{
    [Test]
    public async Task Array_IsEqualTo_With_Different_Contents_Renders_Both_Sides()
    {
        uint[] actual = [2u, 5u, 7u];
        uint[] expected = [2u, 5u, 8u];
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).DoesNotContain("System.UInt32[]");
        await Assert.That(exception.Message).Contains("[2, 5, 8]");
        await Assert.That(exception.Message).Contains("[2, 5, 7]");
    }

    [Test]
    public async Task Array_IsEqualTo_With_Equal_Contents_Different_References_Explains_Reference_Semantics()
    {
        uint[] actual = [2u, 5u, 7u];
        uint[] expected = [2u, 5u, 7u];
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).DoesNotContain("System.UInt32[]");
        await Assert.That(exception.Message).Contains("IsEquivalentTo");
    }

    [Test]
    public async Task IntArray_IsEqualTo_Failure_Renders_Contents()
    {
        int[] actual = [1, 2, 3];
        int[] expected = [4, 5, 6];
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).DoesNotContain("System.Int32[]");
        await Assert.That(exception.Message).Contains("[1, 2, 3]");
        await Assert.That(exception.Message).Contains("[4, 5, 6]");
    }

    [Test]
    public async Task List_IsEqualTo_Failure_Renders_Contents()
    {
        var actual = new List<int> { 1, 2, 3 };
        var expected = new List<int> { 4, 5, 6 };
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("[1, 2, 3]");
        await Assert.That(exception.Message).Contains("[4, 5, 6]");
    }

    [Test]
    public async Task NonReplayable_IEnumerable_IsEqualTo_Failure_Renders_Contents_Once()
    {
        // Regression: iterator-generated sequences (yield) are single-shot. Earlier revision
        // enumerated value+expected twice (FormatValue then SequenceEquals) — the second
        // pass saw an empty sequence and silently misreported "same contents, different ref".
        static IEnumerable<int> Yield(params int[] values)
        {
            foreach (var v in values) yield return v;
        }

        IEnumerable<int> actual = Yield(1, 2, 3);
        IEnumerable<int> expected = Yield(4, 5, 6);
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("[1, 2, 3]");
        await Assert.That(exception.Message).Contains("[4, 5, 6]");
        await Assert.That(exception.Message).DoesNotContain("IsEquivalentTo");
    }

    [Test]
    public async Task LargeArray_IsEqualTo_Failure_Truncates_Contents()
    {
        var actual = Enumerable.Range(1, 15).ToArray();
        var expected = Enumerable.Range(100, 15).ToArray();
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).DoesNotContain("System.Int32[]");
        await Assert.That(exception.Message).Contains("more...");
    }

    [Test]
    public async Task StringArray_IsEqualTo_Failure_Quotes_Items()
    {
        // Prevents ambiguity between null and the literal "null", and keeps whitespace visible.
        string[] actual = ["hello", "world"];
        string[] expected = ["foo", "bar"];
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).Contains("[\"hello\", \"world\"]");
        await Assert.That(exception.Message).Contains("[\"foo\", \"bar\"]");
    }

    [Test]
    public async Task NestedCollection_IsEqualTo_Failure_Recurses_Into_Items()
    {
        int[][] actual = [[1, 2], [3, 4]];
        int[][] expected = [[5, 6], [7, 8]];
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).DoesNotContain("System.Int32[]");
        await Assert.That(exception.Message).Contains("[[1, 2], [3, 4]]");
        await Assert.That(exception.Message).Contains("[[5, 6], [7, 8]]");
    }
}
