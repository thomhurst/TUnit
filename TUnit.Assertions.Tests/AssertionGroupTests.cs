using TUnit.Assertions.AssertionBuilders.Groups;

namespace TUnit.Assertions.Tests;

public class AssertionGroupTests
{
    [Test]
    public async Task Test()
    {
        var value = "CD";

        var cd = AssertionGroup.For(value)
            .WithAssertion(assert => assert.Contains('C'))
            .And(assert => assert.Contains('D'));
        
        var ab = AssertionGroup.ForSameValueAs(cd)
            .WithAssertion(assert => assert.Contains('A'))
            .And(assert => assert.Contains('B'));

        await AssertionGroup.Assert(cd).Or(ab);
    }
    
    [Test]
    public async Task Test2()
    {
        var value = "Foo";
        
        await AssertionGroup.For(value)
            .WithAssertion(assert => assert.IsNotNullOrEmpty())
            .And(assert => assert.IsEqualTo("Foo"));
    }
    
    [Test]
    public async Task Test3()
    {
        var value = "Foo";
        
        var group1 = AssertionGroup.For(value)
            .WithAssertion(assert => assert.IsNullOrEmpty())
            .And(assert => assert.IsEqualTo("Foo"));
        
        var group2 = AssertionGroup.ForSameValueAs(group1)
            .WithAssertion(assert => assert.IsNullOrEmpty())
            .Or(assert => assert.IsEqualTo("Foo"));

        await AssertionGroup.Assert(group1).Or(group2);
    }
    
    [Test]
    public async Task And_Condition_Thows_As_Expected()
    {
        var value = "Foo";
        
        var group1 = AssertionGroup.For(value)
            .WithAssertion(assert => assert.IsNullOrEmpty())
            .And(assert => assert.IsEqualTo("Foo"));
        
        var group2 = AssertionGroup.ForSameValueAs(group1)
            .WithAssertion(assert => assert.IsNullOrEmpty())
            .Or(assert => assert.IsEqualTo("Foo"));

        await Assert.That(async () =>
                await AssertionGroup.Assert(group1).And(group2)
            ).Throws<AssertionException>()
            .And
            .HasMessageStartingWith("""
                                    Expected value to be null or empty
                                     and to be equal to "Foo"

                                    but 'Foo' is not null or empty
                                    """);
    }
}