using TUnit.Assertions.Conditions.Helpers;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for issue #3454: Collection IsEquivalentTo should use structural equality for complex objects
/// </summary>
public class CollectionStructuralEquivalenceTests
{
    [Test]
    public async Task Collections_With_Structurally_Equal_Objects_Are_Equivalent()
    {
        // Arrange
        var a = new Message { Content = "Hello" };
        var b = new Message { Content = "Hello" };
        var listA = new List<Message> { a, a };
        var listB = new List<Message> { b, b };

        // Act & Assert - This should now pass with structural equality
        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Structurally_Different_Objects_Are_Not_Equivalent()
    {
        // Arrange
        var a = new Message { Content = "Hello" };
        var b = new Message { Content = "World" };
        var listA = new List<Message> { a };
        var listB = new List<Message> { b };

        // Act & Assert
        await TUnitAssert.That(listA).IsNotEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Nested_Objects_Are_Equivalent()
    {
        // Arrange
        var listA = new List<MessageWithNested>
        {
            new() { Content = "Hello", Nested = new Message { Content = "World" } }
        };
        var listB = new List<MessageWithNested>
        {
            new() { Content = "Hello", Nested = new Message { Content = "World" } }
        };

        // Act & Assert
        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Different_Nested_Objects_Are_Not_Equivalent()
    {
        // Arrange
        var listA = new List<MessageWithNested>
        {
            new() { Content = "Hello", Nested = new Message { Content = "World" } }
        };
        var listB = new List<MessageWithNested>
        {
            new() { Content = "Hello", Nested = new Message { Content = "Universe" } }
        };

        // Act & Assert
        await TUnitAssert.That(listA).IsNotEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Nested_Collections_Are_Equivalent()
    {
        // Arrange
        var listA = new List<MessageWithCollection>
        {
            new() { Content = "Hello", Messages = [new Message { Content = "A" }, new Message { Content = "B" }] }
        };
        var listB = new List<MessageWithCollection>
        {
            new() { Content = "Hello", Messages = [new Message { Content = "A" }, new Message { Content = "B" }] }
        };

        // Act & Assert
        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Different_Nested_Collections_Are_Not_Equivalent()
    {
        // Arrange
        var listA = new List<MessageWithCollection>
        {
            new() { Content = "Hello", Messages = [new Message { Content = "A" }] }
        };
        var listB = new List<MessageWithCollection>
        {
            new() { Content = "Hello", Messages = [new Message { Content = "B" }] }
        };

        // Act & Assert
        await TUnitAssert.That(listA).IsNotEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_ReferenceEqualityComparer_Uses_Reference_Equality()
    {
        // Arrange
        var a = new Message { Content = "Hello" };
        var b = new Message { Content = "Hello" };
        var listA = new List<Message> { a };
        var listB = new List<Message> { b };

        // Act & Assert - With ReferenceEqualityComparer, these should NOT be equivalent
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.That(listA).IsEquivalentTo(listB).Using(ReferenceEqualityComparer<Message>.Instance)
        );

        await TUnitAssert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Collections_With_Same_Reference_And_ReferenceEqualityComparer_Are_Equivalent()
    {
        // Arrange
        var a = new Message { Content = "Hello" };
        var listA = new List<Message> { a };
        var listB = new List<Message> { a }; // Same reference

        // Act & Assert
        await TUnitAssert.That(listA).IsEquivalentTo(listB).Using(ReferenceEqualityComparer<Message>.Instance);
    }

    [Test]
    public async Task Primitives_Still_Work_With_Structural_Comparer()
    {
        // Arrange
        var listA = new List<int> { 1, 2, 3 };
        var listB = new List<int> { 1, 2, 3 };

        // Act & Assert - Primitives should still work efficiently
        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Strings_Still_Work_With_Structural_Comparer()
    {
        // Arrange
        var listA = new List<string> { "a", "b", "c" };
        var listB = new List<string> { "a", "b", "c" };

        // Act & Assert
        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Equatable_Objects_Use_Equatable_Implementation()
    {
        // Arrange
        var listA = new List<EquatableMessage> { new("Hello"), new("World") };
        var listB = new List<EquatableMessage> { new("Hello"), new("World") };

        // Act & Assert
        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Null_Items_Are_Equivalent()
    {
        // Arrange
        var listA = new List<Message?> { new Message { Content = "Hello" }, null, new Message { Content = "World" } };
        var listB = new List<Message?> { new Message { Content = "Hello" }, null, new Message { Content = "World" } };

        // Act & Assert
        await TUnitAssert.That(listA).IsEquivalentTo(listB);
    }

    [Test]
    public async Task Collections_With_Different_Null_Positions_Are_Not_Equivalent()
    {
        // Arrange
        var listA = new List<Message?> { new Message { Content = "Hello" }, null };
        var listB = new List<Message?> { null, new Message { Content = "Hello" } };

        // Act & Assert
        await TUnitAssert.That(listA).IsNotEquivalentTo(listB);
    }

    [Test]
    public async Task Single_Object_IsEquivalentTo_Still_Works_As_Before()
    {
        // Arrange
        var a = new Message { Content = "Hello" };
        var b = new Message { Content = "Hello" };

        // Act & Assert - Ensure object equivalency still works
        await TUnitAssert.That(a).IsEquivalentTo(b);
    }

    [Test]
    public async Task Collections_With_Custom_Comparer_Uses_Custom_Comparer()
    {
        // Arrange
        var listA = new List<string> { "hello", "world" };
        var listB = new List<string> { "HELLO", "WORLD" };

        // Act & Assert - Should be equivalent with case-insensitive comparer
        await TUnitAssert.That(listA).IsEquivalentTo(listB).Using(StringComparer.OrdinalIgnoreCase);
    }

    // Test classes
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
