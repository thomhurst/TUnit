using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for issue #7132.
/// [AssertionFrom] with an unbound generic type, such as typeof(Memory&lt;&gt;), generated nothing and reported
/// nothing, so the assertion silently did not exist. It now generates assertions that are generic over the
/// type's parameters and work for any closed type.
/// </summary>
public class Tests7132
{
    [Test]
    public async Task Unbound_generic_property_assertion_passes()
    {
        Memory<int> memory = Memory<int>.Empty;

        await Assert.That(memory).IsEmptyMemory();
    }

    [Test]
    public async Task Unbound_generic_property_assertion_fails()
    {
        Memory<int> memory = new[] { 1, 2, 3 };

        await Assert.That(async () => await Assert.That(memory).IsEmptyMemory())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Unbound_generic_negated_assertion_works_for_other_type_arguments()
    {
        ReadOnlyMemory<string> memory = new[] { "a" };

        await Assert.That(memory).IsNotEmptyReadOnlyMemory();
        await Assert.That(async () => await Assert.That(ReadOnlyMemory<string>.Empty).IsNotEmptyReadOnlyMemory())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Unbound_generic_method_with_type_parameter_argument()
    {
        var bag = new Bag<int>(1, 2, 3);

        await Assert.That(bag).Holds(2);
        await Assert.That(async () => await Assert.That(bag).Holds(4))
            .Throws<AssertionException>();
    }

    public class Bag<T>(params T[] items)
    {
        public bool Holds(T item) => Array.IndexOf(items, item) >= 0;
    }
}

[AssertionFrom(typeof(Memory<>), nameof(Memory<int>.IsEmpty), CustomName = "IsEmptyMemory", ExpectationMessage = "be empty")]
[AssertionFrom(typeof(ReadOnlyMemory<>), nameof(ReadOnlyMemory<int>.IsEmpty), CustomName = "IsNotEmptyReadOnlyMemory", NegateLogic = true)]
[AssertionFrom(typeof(Tests7132.Bag<>), nameof(Tests7132.Bag<int>.Holds))]
public static partial class Tests7132Assertions;
