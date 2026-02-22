namespace TUnit.Mocks.Tests;

/// <summary>
/// Tests for the <see cref="Times"/> struct: factory methods, validation, matching, formatting, and equality.
/// </summary>
public class TimesTests
{
    // ─── Factory Methods ────────────────────────────────────────────────────────

    [Test]
    public async Task Once_Matches_Exactly_One()
    {
        var times = Times.Once;
        await Assert.That(times.Matches(0)).IsFalse();
        await Assert.That(times.Matches(1)).IsTrue();
        await Assert.That(times.Matches(2)).IsFalse();
    }

    [Test]
    public async Task Never_Matches_Only_Zero()
    {
        var times = Times.Never;
        await Assert.That(times.Matches(0)).IsTrue();
        await Assert.That(times.Matches(1)).IsFalse();
    }

    [Test]
    public async Task AtLeastOnce_Matches_One_Or_More()
    {
        var times = Times.AtLeastOnce;
        await Assert.That(times.Matches(0)).IsFalse();
        await Assert.That(times.Matches(1)).IsTrue();
        await Assert.That(times.Matches(100)).IsTrue();
    }

    [Test]
    public async Task Exactly_Matches_Only_Specified_Count()
    {
        var times = Times.Exactly(3);
        await Assert.That(times.Matches(2)).IsFalse();
        await Assert.That(times.Matches(3)).IsTrue();
        await Assert.That(times.Matches(4)).IsFalse();
    }

    [Test]
    public async Task Exactly_Zero_Is_Same_As_Never()
    {
        var times = Times.Exactly(0);
        await Assert.That(times.Matches(0)).IsTrue();
        await Assert.That(times.Matches(1)).IsFalse();
        await Assert.That(times).IsEqualTo(Times.Never);
    }

    [Test]
    public async Task AtLeast_Matches_N_Or_More()
    {
        var times = Times.AtLeast(3);
        await Assert.That(times.Matches(2)).IsFalse();
        await Assert.That(times.Matches(3)).IsTrue();
        await Assert.That(times.Matches(1000)).IsTrue();
    }

    [Test]
    public async Task AtMost_Matches_Zero_Through_N()
    {
        var times = Times.AtMost(3);
        await Assert.That(times.Matches(0)).IsTrue();
        await Assert.That(times.Matches(3)).IsTrue();
        await Assert.That(times.Matches(4)).IsFalse();
    }

    [Test]
    public async Task Between_Matches_Inclusive_Range()
    {
        var times = Times.Between(2, 5);
        await Assert.That(times.Matches(1)).IsFalse();
        await Assert.That(times.Matches(2)).IsTrue();
        await Assert.That(times.Matches(5)).IsTrue();
        await Assert.That(times.Matches(6)).IsFalse();
    }

    // ─── Validation / Error Paths ───────────────────────────────────────────────

    [Test]
    public async Task Exactly_Negative_Throws_ArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Times.Exactly(-1));
        await Assert.That(exception.ParamName).IsEqualTo("n");
    }

    [Test]
    public async Task AtLeast_Negative_Throws_ArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Times.AtLeast(-1));
        await Assert.That(exception.ParamName).IsEqualTo("n");
    }

    [Test]
    public async Task AtMost_Negative_Throws_ArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Times.AtMost(-1));
        await Assert.That(exception.ParamName).IsEqualTo("n");
    }

    [Test]
    public async Task Between_Negative_Min_Throws_ArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Times.Between(-1, 5));
        await Assert.That(exception.ParamName).IsEqualTo("min");
    }

    [Test]
    public async Task Between_Max_Less_Than_Min_Throws_ArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Times.Between(5, 3));
        await Assert.That(exception.ParamName).IsEqualTo("max");
    }

    // ─── ToString ───────────────────────────────────────────────────────────────

    [Test]
    public async Task ToString_Never()
    {
        await Assert.That(Times.Never.ToString()).IsEqualTo("never");
    }

    [Test]
    public async Task ToString_Once()
    {
        await Assert.That(Times.Once.ToString()).IsEqualTo("exactly once");
    }

    [Test]
    public async Task ToString_Exactly_N()
    {
        await Assert.That(Times.Exactly(3).ToString()).IsEqualTo("exactly 3 times");
    }

    [Test]
    public async Task ToString_AtLeast()
    {
        await Assert.That(Times.AtLeast(2).ToString()).IsEqualTo("at least 2 times");
    }

    [Test]
    public async Task ToString_AtMost()
    {
        await Assert.That(Times.AtMost(5).ToString()).IsEqualTo("at most 5 times");
    }

    [Test]
    public async Task ToString_Between()
    {
        await Assert.That(Times.Between(2, 5).ToString()).IsEqualTo("between 2 and 5 times");
    }

    // ─── Equality ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Equals_Same_Values_Returns_True()
    {
        var a = Times.Exactly(3);
        var b = Times.Exactly(3);
        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
    }

    [Test]
    public async Task Equals_Different_Values_Returns_False()
    {
        var a = Times.Exactly(3);
        var b = Times.Exactly(4);
        await Assert.That(a.Equals(b)).IsFalse();
        await Assert.That(a == b).IsFalse();
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task Equals_Object_Overload()
    {
        object a = Times.Once;
        object b = Times.Once;
        await Assert.That(a.Equals(b)).IsTrue();
    }

    [Test]
    public async Task Equals_Object_Null_Returns_False()
    {
        var a = Times.Once;
        await Assert.That(a.Equals(null)).IsFalse();
    }

    [Test]
    public async Task Equals_Object_Wrong_Type_Returns_False()
    {
        var a = Times.Once;
        await Assert.That(a.Equals("not a Times")).IsFalse();
    }

    [Test]
    public async Task GetHashCode_Equal_Instances_Have_Same_Hash()
    {
        var a = Times.Exactly(3);
        var b = Times.Exactly(3);
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task GetHashCode_Different_Instances_Have_Different_Hash()
    {
        // Not strictly guaranteed but highly likely for different values
        var a = Times.Exactly(3);
        var b = Times.AtLeast(3);
        await Assert.That(a.GetHashCode()).IsNotEqualTo(b.GetHashCode());
    }
}
