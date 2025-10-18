#if NET6_0_OR_GREATER
namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for IEquatable&lt;T&gt; assertions where the actual and expected types differ.
/// These tests verify that types implementing IEquatable with cross-type equality work correctly.
/// Note: Only available on .NET 6+ due to overload resolution limitations in older frameworks.
/// </summary>
public class EquatableTests
{
    // Example struct from GitHub issue #2972
    public struct Wrapper : IEquatable<Wrapper>, IEquatable<long>
    {
        public long Value { get; set; }

        public bool Equals(Wrapper other) => Value == other.Value;

        public bool Equals(long other) => Value == other;

        public override bool Equals(object? obj)
        {
            return obj switch
            {
                Wrapper wrapper => Equals(wrapper),
                long l => Equals(l),
                _ => false
            };
        }

        public override int GetHashCode() => Value.GetHashCode();
    }

    // Struct with multiple IEquatable implementations
    public struct MultiEquatable : IEquatable<MultiEquatable>, IEquatable<int>, IEquatable<string>
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }

        public bool Equals(MultiEquatable other) =>
            IntValue == other.IntValue && StringValue == other.StringValue;

        public bool Equals(int other) => IntValue == other;

        public bool Equals(string? other) => StringValue == other;

        public override bool Equals(object? obj)
        {
            return obj switch
            {
                MultiEquatable m => Equals(m),
                int i => Equals(i),
                string s => Equals(s),
                _ => false
            };
        }

        public override int GetHashCode() => HashCode.Combine(IntValue, StringValue);
    }

    // Simple struct with IEquatable<int>
    public struct IntWrapper : IEquatable<int>
    {
        public int Value { get; set; }

        public bool Equals(int other) => Value == other;

        public override bool Equals(object? obj) => obj is int i && Equals(i);

        public override int GetHashCode() => Value.GetHashCode();
    }

    [Test]
    public async Task Wrapper_IsEqualTo_Long_Success()
    {
        // Arrange
        Wrapper wrapper = new() { Value = 42 };

        // Act & Assert
        await Assert.That(wrapper).IsEquatableTo(42L);
    }

    [Test]
    public async Task Wrapper_IsEqualTo_Long_Failure()
    {
        // Arrange
        Wrapper wrapper = new() { Value = 42 };

        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(wrapper).IsEquatableTo(99L);
        });
    }

    [Test]
    public async Task Wrapper_IsEqualTo_Wrapper_Success()
    {
        // Arrange
        Wrapper wrapper1 = new() { Value = 42 };
        Wrapper wrapper2 = new() { Value = 42 };

        // Act & Assert
        await Assert.That(wrapper1).IsEqualTo(wrapper2);
    }

    [Test]
    public async Task NullableWrapper_IsEqualTo_Long_Success()
    {
        // Arrange
        Wrapper? wrapper = new Wrapper { Value = 42 };

        // Act & Assert
        await Assert.That(wrapper).IsEquatableTo(42L);
    }

    [Test]
    public async Task NullableWrapper_IsEqualTo_Long_NullFailure()
    {
        // Arrange
        Wrapper? wrapper = null;

        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(wrapper).IsEquatableTo(42L);
        });
    }

    [Test]
    public async Task MultiEquatable_IsEqualTo_Int_Success()
    {
        // Arrange
        MultiEquatable value = new() { IntValue = 100, StringValue = "test" };

        // Act & Assert
        await Assert.That(value).IsEquatableTo(100);
    }

    [Test]
    public async Task MultiEquatable_IsEqualTo_String_Success()
    {
        // Arrange
        MultiEquatable value = new() { IntValue = 100, StringValue = "test" };

        // Act & Assert
        await Assert.That(value).IsEquatableTo("test");
    }

    [Test]
    public async Task IntWrapper_IsEqualTo_Int_Success()
    {
        // Arrange
        IntWrapper wrapper = new() { Value = 123 };

        // Act & Assert
        await Assert.That(wrapper).IsEquatableTo(123);
    }

    [Test]
    public async Task IntWrapper_IsEqualTo_Int_Failure()
    {
        // Arrange
        IntWrapper wrapper = new() { Value = 123 };

        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(wrapper).IsEquatableTo(456);
        });
    }

    [Test]
    public async Task EquatableAssertion_WithAnd_Success()
    {
        // Arrange
        Wrapper wrapper = new() { Value = 42 };

        // Act & Assert
        await Assert.That(wrapper).IsEquatableTo(42L).And.IsEqualTo(new Wrapper { Value = 42 });
    }

    [Test]
    public async Task EquatableAssertion_WithOr_Success()
    {
        // Arrange
        Wrapper wrapper = new() { Value = 42 };

        // Act & Assert - should pass because first condition is true
        await Assert.That(wrapper).IsEquatableTo(42L).Or.IsEqualTo(new Wrapper { Value = 99 });
    }

    [Test]
    public async Task EquatableAssertion_WithAnd_Failure()
    {
        // Arrange
        Wrapper wrapper = new() { Value = 42 };

        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(wrapper).IsEquatableTo(42L).And.IsEqualTo(new Wrapper { Value = 99 });
        });
    }

    [Test]
    public async Task EquatableAssertion_ChainedWithAndContinuation()
    {
        // Arrange
        Wrapper wrapper = new() { Value = 42 };

        // Act & Assert - test that And continuation works
        await Assert.That(wrapper).IsEquatableTo(42L).And.IsEquatableTo(42L);
    }

    [Test]
    public async Task EquatableAssertion_ChainedWithOrContinuation()
    {
        // Arrange
        Wrapper wrapper = new() { Value = 42 };

        // Act & Assert - test that Or continuation works
        await Assert.That(wrapper).IsEquatableTo(99L).Or.IsEquatableTo(42L);
    }

    [Test]
    public async Task NullableIntWrapper_IsEqualTo_Int_Success()
    {
        // Arrange
        IntWrapper? wrapper = new IntWrapper { Value = 789 };

        // Act & Assert
        await Assert.That(wrapper).IsEquatableTo(789);
    }

    [Test]
    public async Task NullableIntWrapper_IsEqualTo_Int_NullFailure()
    {
        // Arrange
        IntWrapper? wrapper = null;

        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(wrapper).IsEquatableTo(789);
        });
    }

    // Edge case: Ensure the constraint-based overload doesn't interfere with standard equality
    [Test]
    public async Task StandardEquality_StillWorks_ForSameTypes()
    {
        // Arrange
        int value = 42;

        // Act & Assert - should use standard IsEqualTo, not the IEquatable overload
        await Assert.That(value).IsEqualTo(42);
    }

    // Test to verify the example from GitHub issue #2972 works exactly as requested
    [Test]
    public async Task GitHubIssue2972_Example()
    {
        // This is the exact example from the GitHub issue
        Wrapper value = new() { Value = 1 };
        await Assert.That(value).IsEquatableTo(1L);
    }
}
#endif
