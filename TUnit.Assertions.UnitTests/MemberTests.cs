using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;


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
    public void Number_Falsey()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = false
        };

        var exception = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(myClass).HasMember(x => x.Number).EqualTo(1));
        NUnitAssert.That(exception, Has.Message.EqualTo(
            """
            Expected myClass MyClass.Number to be equal to 1
            
            but received 123
            
            at Assert.That(myClass).HasMember(x => x.Number).EqualTo(1)
            """
            ));
    }
    
    [Test]
    public void Number_Nested_Falsey()
    {
        var myClass = new MyClass
        {
            Number = 123,
            Text = "Blah",
            Flag = false
        };

        var exception = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(myClass).HasMember(x => x.Nested.Nested.Nested.Number).EqualTo(1));
        NUnitAssert.That(exception, Has.Message.EqualTo(
            """
            Expected myClass MyClass.Number to be equal to 1
            
            but received 123
            
            at Assert.That(myClass).HasMember(x => x.Nested.Nested.Nested.Number).EqualTo(1)
            """
        ));
    }
    
    [Test]
    public void Number_Null()
    {
        MyClass myClass = null!;

        var exception = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(myClass).HasMember(x => x.Number).EqualTo(1));
        NUnitAssert.That(exception, Has.Message.EqualTo(
            """
            Expected myClass MyClass.Number to be equal to 1
            
            but Object `MyClass` was null
            
            at Assert.That(myClass).HasMember(x => x.Number).EqualTo(1)
            """
        ));
    }

    private class MyClass
    {
        public required int Number { get; init; }
        public required string Text { get; init; }
        public required bool Flag { get; init; }

        public MyClass Nested => this;
    }
}