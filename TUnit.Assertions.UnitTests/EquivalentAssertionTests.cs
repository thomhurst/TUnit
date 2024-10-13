using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class EquivalentAssertionTests
{
    [Test]
    public async Task Basic_Objects_Are_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo"
        };
        var object2 = new MyClass
        {
            Value = "Foo"
        };

        await TUnitAssert.That(object1).IsEquivalentTo(object2);
    }
    
    [Test]
    public void Mismatched_Objects_Are_Not_Equivalent()
    {
        var object1 = new MyClass();
        var object2 = new MyClass
        {
            Value = "Foo"
        };

        var exception = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(object1).IsEquivalentTo(object2));
        
        NUnitAssert.That(exception!.Message, Is.EqualTo(
            """
            Expected object1 to be equivalent to object2
            
            but Property Value did not match
            Expected: "Foo"
            Received: null
            
            at Assert.That(object1).IsEquivalentTo(object2)
            """
            ));
    }
    
    [Test]
    public void Objects_With_Nested_Mismatch_Are_Not_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass()
            }
        };
        var object2 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz"
                }
            }
        };

        var exception = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(object1).IsEquivalentTo(object2));
        
        NUnitAssert.That(exception!.Message, Is.EqualTo(
            """
            Expected object1 to be equivalent to object2
            
            but Property Inner.Inner.Value did not match
            Expected: "Baz"
            Received: null
            
            at Assert.That(object1).IsEquivalentTo(object2)
            """
        ));
    }
  
    [Test]
    public async Task Objects_With_Nested_Matches_Are_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz"
                }
            }
        };
        var object2 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz"
                }
            }
        };

        await TUnitAssert.That(object1).IsEquivalentTo(object2);
    }
    
    [Test]
    public void Objects_With_Nested_Enumerable_Mismatch_Are_Not_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz",
                    Collection = [ "1", "2", "3" ]
                }
            }
        };
        
        var object2 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz",
                    Collection = [ "1", "2", "3", "4" ]
                }
            }
        };

        var exception = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(object1).IsEquivalentTo(object2));
        
        NUnitAssert.That(exception!.Message, Is.EqualTo(
            """
            Expected object1 to be equivalent to object2
            
            but EnumerableItem Inner.Inner.Collection.[3] did not match
            Expected: "4"
            Received: null
            
            at Assert.That(object1).IsEquivalentTo(object2)
            """
        ));
    }
    
    [Test]
    public async Task Objects_With_Nested_Enumerable_Mismatch_With_Ignore_Rule_Are_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz",
                    Collection = [ "1", "2", "3" ]
                }
            }
        };
        
        var object2 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz",
                    Collection = [ "1", "2", "3", "4" ]
                }
            }
        };

        await TUnitAssert.That(object1).IsEquivalentTo(object2).IgnoringMember("Inner.Inner.Collection.[3]");
    }

    [Test]
    public async Task Objects_With_Nested_Enumerable_Matches_Are_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz",
                    Collection = [ "1", "2", "3" ]
                }
            }
        };
        var object2 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar",
                Inner = new InnerClass
                {
                    Value = "Baz",
                    Collection = [ "1", "2", "3" ]
                }
            }
        };

        await TUnitAssert.That(object1).IsEquivalentTo(object2);
    }
    
    public class MyClass
    {
        public string? Value { get; set; }
        public InnerClass? Inner { get; set; }
    }

    public class InnerClass
    {
        public string? Value { get; set; }
        
        public InnerClass? Inner { get; set; }
        
        public IEnumerable<string>? Collection { get; set; } 
    }
}