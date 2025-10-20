using TUnit.Assertions.Conditions.Helpers;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for issue #3454: Collection IsEquivalentTo should use structural equality for complex objects
/// </summary>
public class CollectionStructuralEquivalenceTests
{
    [Test]
    public async Task Collections_With_Structurally_Equal_Objects_Are_Equivalent()
    {
        var a = new Message { Content = "Hello" };
        var b = new Message { Content = "Hello" };
        var listA = new List<Message> { a, a };
        var listB = new List<Message> { b, b };

        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Structurally_Different_Objects_Are_Not_Equivalent()
    {
        var a = new Message { Content = "Hello" };
        var b = new Message { Content = "World" };
        var listA = new List<Message> { a };
        var listB = new List<Message> { b };

        await TUnitAssert.That(listA).IsNotEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Nested_Objects_Are_Equivalent()
    {
        var listA = new List<MessageWithNested>
        {
            new() { Content = "Hello", Nested = new Message { Content = "World" } }
        };
        var listB = new List<MessageWithNested>
        {
            new() { Content = "Hello", Nested = new Message { Content = "World" } }
        };

        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Different_Nested_Objects_Are_Not_Equivalent()
    {
        var listA = new List<MessageWithNested>
        {
            new() { Content = "Hello", Nested = new Message { Content = "World" } }
        };
        var listB = new List<MessageWithNested>
        {
            new() { Content = "Hello", Nested = new Message { Content = "Universe" } }
        };

        await TUnitAssert.That(listA).IsNotEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Nested_Collections_Are_Equivalent()
    {
        var listA = new List<MessageWithCollection>
        {
            new() { Content = "Hello", Messages = [new Message { Content = "A" }, new Message { Content = "B" }] }
        };
        var listB = new List<MessageWithCollection>
        {
            new() { Content = "Hello", Messages = [new Message { Content = "A" }, new Message { Content = "B" }] }
        };

        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Different_Nested_Collections_Are_Not_Equivalent()
    {
        var listA = new List<MessageWithCollection>
        {
            new() { Content = "Hello", Messages = [new Message { Content = "A" }] }
        };
        var listB = new List<MessageWithCollection>
        {
            new() { Content = "Hello", Messages = [new Message { Content = "B" }] }
        };

        await TUnitAssert.That(listA).IsNotEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_ReferenceEqualityComparer_Uses_Reference_Equality()
    {
        var a = new Message { Content = "Hello" };
        var b = new Message { Content = "Hello" };
        var listA = new List<Message> { a };
        var listB = new List<Message> { b };

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.That(listA).IsEquivalentTo(listB).Using(ReferenceEqualityComparer<Message>.Instance)
        );

        await TUnitAssert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Collections_With_Same_Reference_And_ReferenceEqualityComparer_Are_Equivalent()
    {
        var a = new Message { Content = "Hello" };
        var listA = new List<Message> { a };
        var listB = new List<Message> { a };

        await TUnitAssert.That(listA).IsEquivalentTo(listB).Using(ReferenceEqualityComparer<Message>.Instance);
    }

    [Test]
    public async Task Primitives_Still_Work_With_Structural_Comparer()
    {
        var listA = new List<int> { 1, 2, 3 };
        var listB = new List<int> { 1, 2, 3 };

        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Strings_Still_Work_With_Structural_Comparer()
    {
        var listA = new List<string> { "a", "b", "c" };
        var listB = new List<string> { "a", "b", "c" };

        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Equatable_Objects_Use_Equatable_Implementation()
    {
        var listA = new List<EquatableMessage> { new("Hello"), new("World") };
        var listB = new List<EquatableMessage> { new("Hello"), new("World") };

        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Null_Items_Are_Equivalent()
    {
        var listA = new List<Message?> { new Message { Content = "Hello" }, null, new Message { Content = "World" } };
        var listB = new List<Message?> { new Message { Content = "Hello" }, null, new Message { Content = "World" } };

        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Different_Null_Positions_Are_Equivalent_By_Default()
    {
        var listA = new List<Message?> { new Message { Content = "Hello" }, null };
        var listB = new List<Message?> { null, new Message { Content = "Hello" } };

        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Different_Null_Positions_Are_Not_Equivalent_When_Order_Matters()
    {
        var listA = new List<Message?> { new Message { Content = "Hello" }, null };
        var listB = new List<Message?> { null, new Message { Content = "Hello" } };

        await TUnitAssert.That(listA).IsNotEquivalentTo(listB, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Single_Object_IsEquivalentTo_Still_Works_As_Before()
    {
        var a = new Message { Content = "Hello" };
        var b = new Message { Content = "Hello" };

        await TUnitAssert.That(a).IsEquivalentTo(b);
    }

    [Test]
    public async Task Collections_With_Custom_Comparer_Uses_Custom_Comparer()
    {
        var listA = new List<string> { "hello", "world" };
        var listB = new List<string> { "HELLO", "WORLD" };

        await TUnitAssert.That(listA).IsEquivalentTo(listB).Using(StringComparer.OrdinalIgnoreCase);
    }
    public class Message
    {
        public string? Content { get; set; }
    }

    public class MessageWithNested
    {
        public string? Content { get; set; }
        public Message? Nested { get; set; }
    }

    public class MessageWithCollection
    {
        public string? Content { get; set; }
        public List<Message>? Messages { get; set; }
    }

    public class EquatableMessage : IEquatable<EquatableMessage>
    {
        public string Content { get; }

        public EquatableMessage(string content)
        {
            Content = content;
        }

        public bool Equals(EquatableMessage? other)
        {
            if (other == null)
            {
                return false;
            }

            return Content == other.Content;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EquatableMessage);
        }

        public override int GetHashCode()
        {
            return Content.GetHashCode();
        }
    }
}
