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
    public async Task LargeArray_IsEqualTo_Failure_Truncates_Contents()
    {
        var actual = Enumerable.Range(1, 15).ToArray();
        var expected = Enumerable.Range(100, 15).ToArray();
        var action = async () => await Assert.That(actual).IsEqualTo(expected);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message).DoesNotContain("System.Int32[]");
        await Assert.That(exception.Message).Contains("more...");
    }
}
