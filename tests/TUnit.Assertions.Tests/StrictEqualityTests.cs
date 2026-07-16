namespace TUnit.Assertions.Tests;

public class StrictEqualityTests
{
    [Test]
    public async Task IsStrictlyEqualTo_Passes_For_Equal_Values()
    {
        var value = 42;
        await Assert.That(value).IsStrictlyEqualTo(42);
    }

    [Test]
    public async Task IsStrictlyEqualTo_Passes_For_Equal_Strings()
    {
        var value = "hello";
        await Assert.That(value).IsStrictlyEqualTo("hello");
    }

    [Test]
    public async Task IsStrictlyEqualTo_Passes_For_Same_Reference()
    {
        var obj = new object();
        await Assert.That(obj).IsStrictlyEqualTo(obj);
    }

    [Test]
    public async Task IsStrictlyEqualTo_Fails_For_Different_Values()
    {
        var value = 42;
        await Assert.That(async () => await Assert.That(value).IsStrictlyEqualTo(43))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsStrictlyEqualTo_Fails_For_Different_References()
    {
        var obj1 = new NoEqualsOverride();
        var obj2 = new NoEqualsOverride();

        // Without Equals override, different instances are not equal
        await Assert.That(async () => await Assert.That(obj1).IsStrictlyEqualTo(obj2))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsStrictlyEqualTo_Uses_Object_Equals_Not_IEquatable()
    {
        // CustomEquatable uses IEquatable<T> to say all instances are equal
        // But object.Equals is NOT overridden, so strict equality should fail
        var obj1 = new CustomEquatable(1);
        var obj2 = new CustomEquatable(2);

        // Standard equality would pass (IEquatable says they're equal)
        await Assert.That(obj1).IsEqualTo(obj2);

        // Strict equality should fail (object.Equals returns false for different refs)
        await Assert.That(async () => await Assert.That(obj1).IsStrictlyEqualTo(obj2))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNotStrictlyEqualTo_Passes_For_Different_Values()
    {
        var value = 42;
        await Assert.That(value).IsNotStrictlyEqualTo(43);
    }

    [Test]
    public async Task IsNotStrictlyEqualTo_Passes_For_Different_References()
    {
        var obj1 = new NoEqualsOverride();
        var obj2 = new NoEqualsOverride();
        await Assert.That(obj1).IsNotStrictlyEqualTo(obj2);
    }

    [Test]
    public async Task IsNotStrictlyEqualTo_Fails_For_Equal_Values()
    {
        var value = 42;
        await Assert.That(async () => await Assert.That(value).IsNotStrictlyEqualTo(42))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNotStrictlyEqualTo_Fails_For_Same_Reference()
    {
        var obj = new object();
        await Assert.That(async () => await Assert.That(obj).IsNotStrictlyEqualTo(obj))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsStrictlyEqualTo_Handles_Null_Values()
    {
        string? value = null;
        await Assert.That(value).IsStrictlyEqualTo(null);
    }

    [Test]
    public async Task IsStrictlyEqualTo_Fails_When_Only_One_Is_Null()
    {
        string? value = "hello";
        await Assert.That(async () => await Assert.That(value).IsStrictlyEqualTo(null))
            .Throws<AssertionException>();
    }

    // Helper class that does NOT override Equals
    private class NoEqualsOverride
    {
    }

    // Helper class that implements IEquatable<T> but doesn't override object.Equals
    private class CustomEquatable : IEquatable<CustomEquatable>
    {
        public int Value { get; }

        public CustomEquatable(int value)
        {
            Value = value;
        }

        // IEquatable says all instances are equal
        public bool Equals(CustomEquatable? other) => true;

        // Deliberately NOT overriding object.Equals
        // So object.Equals(a, b) uses reference equality
    }
}
