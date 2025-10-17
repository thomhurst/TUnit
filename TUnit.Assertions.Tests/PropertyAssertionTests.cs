namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for the HasProperty sugar syntax, which provides a simpler alternative
/// to .Member() for common property assertion scenarios.
/// </summary>
public class PropertyAssertionTests
{
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? MiddleName { get; set; }
    }

    public class CustomCollection : List<string>
    {
        public string Title { get; set; } = string.Empty;
        public int Version { get; set; }
    }

    [Test]
    public async Task HasProperty_SimpleEquality_Works()
    {
        var person = new Person { Name = "Alice", Age = 30 };

        await Assert.That(person).HasProperty(x => x.Name, "Alice");
    }

    [Test]
    public async Task HasProperty_FluentIsEqualTo_Works()
    {
        var person = new Person { Name = "Bob", Age = 25 };

        // Fluent API - assertion executes when awaited
        await Assert.That(person).HasProperty(x => x.Name).IsEqualTo("Bob");
    }

    [Test]
    public async Task HasProperty_FluentIsNotEqualTo_Works()
    {
        var person = new Person { Name = "Charlie", Age = 35 };

        await Assert.That(person).HasProperty(x => x.Name).IsNotEqualTo("David");
    }

    [Test]
    public async Task HasProperty_FluentIsNull_Works()
    {
        var person = new Person { Name = "Eve", MiddleName = null };

        await Assert.That(person).HasProperty(x => x.MiddleName).IsNull();
    }

    [Test]
    public async Task HasProperty_FluentIsNotNull_Works()
    {
        var person = new Person { Name = "Frank", MiddleName = "James" };

        await Assert.That(person).HasProperty(x => x.MiddleName).IsNotNull();
    }

    [Test]
    public async Task HasProperty_MultipleProperties_Works()
    {
        var person = new Person { Name = "Grace", Age = 40 };

        await Assert.That(person)
            .HasProperty(x => x.Name, "Grace")
            .And.HasProperty(x => x.Age, 40);
    }

    [Test]
    public async Task HasProperty_OnCustomCollection_Works()
    {
        var collection = new CustomCollection { "A", "B", "C" };
        collection.Title = "Alphabet";
        collection.Version = 1;

        // Can assert on both collection properties AND collection contents
        await Assert.That(collection)
            .HasProperty(x => x.Title, "Alphabet")
            .And.HasProperty(x => x.Version, 1)
            .And.Contains("A");
    }

    [Test]
    public async Task HasProperty_FluentChain_Works()
    {
        var person = new Person { Name = "Helen", Age = 28 };

        // Use simple syntax for chaining
        await Assert.That(person)
            .HasProperty(x => x.Name, "Helen")
            .And.HasProperty(x => x.Age, 28);
    }

    [Test]
    public async Task HasProperty_WithIntProperty_Works()
    {
        var person = new Person { Name = "Ivan", Age = 50 };

        await Assert.That(person).HasProperty(x => x.Age, 50);
    }

    [Test]
    public async Task HasProperty_SimpleVsFluent_BothWork()
    {
        var person = new Person { Name = "Julia", Age = 33 };

        // Simple syntax
        await Assert.That(person).HasProperty(x => x.Name, "Julia");

        // Fluent syntax (same result)
        await Assert.That(person).HasProperty(x => x.Name).IsEqualTo("Julia");
    }

    [Test]
    public async Task HasProperty_ComparedToMember_ProducesSameResult()
    {
        var person = new Person { Name = "Kevin", Age = 45 };

        // HasProperty is sugar for Member
        await Assert.That(person).HasProperty(x => x.Name, "Kevin");

        // Equivalent to:
        await Assert.That(person).Member(x => x.Name, name => name.IsEqualTo("Kevin"));
    }
}
