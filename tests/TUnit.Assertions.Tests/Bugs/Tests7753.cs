using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7753:
/// IsEquivalentTo had no reference-equality check, so it read every member of one instance twice
/// and failed when a getter returned a different value on its second read. An object could fail
/// to be equivalent to itself, and a collection to a collection of the very same objects.
/// </summary>
public class Tests7753
{
    // A getter that gives a different value at each read.
    public sealed class Ticking
    {
        private int _reads;

        public int Value => ++_reads;
    }

    public sealed class Holder(Ticking child)
    {
        public Ticking Child { get; } = child;
    }

    [Test]
    public async Task Object_Is_Equivalent_To_Itself()
    {
        var item = new Ticking();

        await Assert.That(item).IsEquivalentTo(item);
    }

    [Test]
    public async Task Object_Is_Not_Reported_As_Not_Equivalent_To_Itself()
    {
        var item = new Ticking();

        await Assert.That(async () => await Assert.That(item).IsNotEquivalentTo(item))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Objects_Sharing_A_Member_Instance_Are_Equivalent()
    {
        var item = new Ticking();

        await Assert.That(new Holder(item)).IsEquivalentTo(new Holder(item));
    }

    [Test]
    public async Task Collection_Is_Equivalent_To_A_Collection_Of_The_Same_Objects()
    {
        var item = new Ticking();
        Ticking[] actual = [item];
        Ticking[] expected = [item];

        await Assert.That(actual).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Collection_Is_Equivalent_To_A_Collection_Of_The_Same_Objects_In_Matching_Order()
    {
        var first = new Ticking();
        var second = new Ticking();
        Ticking[] actual = [first, second];
        Ticking[] expected = [first, second];

        await Assert.That(actual).IsEquivalentTo(expected, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Collection_Is_Not_Reported_As_Not_Equivalent_To_A_Collection_Of_The_Same_Objects()
    {
        var item = new Ticking();
        Ticking[] actual = [item];
        Ticking[] expected = [item];

        await Assert.That(async () => await Assert.That(actual).IsNotEquivalentTo(expected))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Collections_Of_Objects_Sharing_A_Member_Instance_Are_Equivalent()
    {
        var item = new Ticking();
        Holder[] actual = [new Holder(item)];
        Holder[] expected = [new Holder(item)];

        await Assert.That(actual).IsEquivalentTo(expected);
    }
}
