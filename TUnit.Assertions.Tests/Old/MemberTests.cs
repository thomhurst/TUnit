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

        await TUnitAssert.That(myClass).HasMember(x => x.Number).EqualTo(123);
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

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(myClass).HasMember(x => x.Number).EqualTo(1));
        await TUnitAssert.That(exception).HasMessageEqualTo(
            """
            Expected myClass MyClass.Number to be equal to 1
            
            but received 123
            
            at Assert.That(myClass).HasMember(x => x.Number).EqualTo(1)
            """
            );
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

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(myClass).HasMember(x => x.Nested.Nested.Nested.Number).EqualTo(1));
        await TUnitAssert.That(exception).HasMessageEqualTo(
            """
            Expected myClass MyClass.Number to be equal to 1
            
            but received 123
            
            at Assert.That(myClass).HasMember(x => x.Nested.Nested.Nested.Number).EqualTo(1)
            """
        );
    }
    
    [Test]
    public async Task Number_Null()
    {
        MyClass myClass = null!;

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(myClass).HasMember(x => x.Number).EqualTo(1));
        await TUnitAssert.That(exception).HasMessageEqualTo(
            """
            Expected myClass MyClass.Number to be equal to 1
            
            but Object `MyClass` was null
            
            at Assert.That(myClass).HasMember(x => x.Number).EqualTo(1)
            """
        );
    }

    private class MyClass
    {
        public required int Number { get; init; }
        public required string Text { get; init; }
        public required bool Flag { get; init; }

        public MyClass Nested => this;
    }
}