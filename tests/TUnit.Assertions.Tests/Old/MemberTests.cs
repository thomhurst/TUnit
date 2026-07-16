namespace TUnit.Assertions.Tests.Old;


public class MemberTests
{
    [Test]
    public async Task Number_Truthy()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = false
        };

        await TUnitAssert.That(myClass).Member(x => x.Number, num => num.IsEqualTo(123));
    }

    [Test]
    public async Task Number_Falsey()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = false
        };

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(myClass).Member(x => x.Number, num => num.IsEqualTo(1)));

        await TUnitAssert.That(exception.Message).Contains("to be 1");
        await TUnitAssert.That(exception.Message).Contains("but found 123");
    }

    [Test]
    public async Task Number_Nested_Falsey()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = false
        };

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(myClass).Member(x => x.Nested.Nested.Nested.Number, num => num.IsEqualTo(1)));

        await TUnitAssert.That(exception.Message).Contains("to be 1");
        await TUnitAssert.That(exception.Message).Contains("but found 123");
    }

    [Test]
    public async Task Number_Null()
    {
        MyClass myClass = null!;

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(myClass).Member(x => x.Number, num => num.IsEqualTo(1)));

        await TUnitAssert.That(exception.Message).Contains("InvalidOperationException");
    }

    [Test]
    public async Task Multiple_HasMember_Chained_With_And()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = true
        };

        await TUnitAssert.That(myClass)
            .Member(x => x.Number, num => num.IsEqualTo(123))
            .And.Member(x => x.Text, text => text.IsEqualTo("Blah"))
            .And.Member(x => x.Flag, flag => flag.IsTrue());
    }

    [Test]
    public async Task Multiple_HasMember_Chained_Second_Fails()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = true
        };

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(myClass)
                .Member(x => x.Number, num => num.IsEqualTo(123))
                .And.Member(x => x.Text, text => text.IsEqualTo("Wrong")));

        await TUnitAssert.That(exception.Message).Contains("to be equal to \"Wrong\"");
    }

    [Test]
    public async Task Multiple_HasMember_Chained_First_Fails()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = true
        };

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(myClass)
                .Member(x => x.Number, num => num.IsEqualTo(999))
                .And.Member(x => x.Text, text => text.IsEqualTo("Blah")));

        await TUnitAssert.That(exception.Message).Contains("to be 999");
    }

    [Test]
    public async Task Multiple_HasMember_Chained_With_Or()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = true
        };

        await TUnitAssert.That(myClass)
            .Member(x => x.Number, num => num.IsEqualTo(999))
            .Or.Member(x => x.Text, text => text.IsEqualTo("Blah"));
    }

    [Test]
    public async Task Chained_HasMember_With_IsNotNull()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = true
        };

        await TUnitAssert.That(myClass)
            .IsNotNull()
            .And.Member(x => x.Number, num => num.IsEqualTo(123))
            .And.Member(x => x.Text, text => text.IsEqualTo("Blah"));
    }

    [Test]
    public async Task Chained_HasMember_Different_Types()
    {
        var complexObject = new ComplexClass
        {
            Name = "Test",
            Age = 25,
            IsActive = true,
            Tags = ["tag1", "tag2"]
        };

        await TUnitAssert.That(complexObject)
            .Member(x => x.Name, name => name.IsEqualTo("Test"))
            .And.Member(x => x.Age, age => age.IsGreaterThan(18))
            .And.Member(x => x.IsActive, active => active.IsTrue());

        // Assert on collection contents separately
        await TUnitAssert.That(complexObject.Tags).Contains("tag1");
    }

    private class MyClass
    {
        public required int Number { get; init; }
        public required string Text { get; init; }
        public required bool Flag { get; init; }

        public MyClass Nested => this;
    }

    private class ComplexClass
    {
        public required string Name { get; init; }
        public required int Age { get; init; }
        public required bool IsActive { get; init; }
        public required List<string> Tags { get; init; }
    }
}
