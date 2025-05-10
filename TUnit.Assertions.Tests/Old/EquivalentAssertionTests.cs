using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests.Old;

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
    public async Task Basic_Objects_Are_Not_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo"
        };
        var object2 = new MyClass
        {
            Value = "Bar"
        };

        await TUnitAssert.That(object1).IsNotEquivalentTo(object2);
    }

    [Test]
    public async Task Different_Objects_Still_Are_Equivalent()
    {
        var result1 = new MyClass
        {
            Value = "Foo"
        };
        
        var result2 = new { Value = "Foo" };

        await TUnitAssert.That(result1).IsEquivalentTo(result2);
    }
    
    [Test]
    public async Task Different_Objects_Are_Not_Equivalent()
    {
        var result1 = new MyClass
        {
            Value = "Foo"
        };
        
        var result2 = new { Value = "Bar" };

        await TUnitAssert.That(result1).IsNotEquivalentTo(result2);
    }
    
    [Test]
    public async Task Different_Enumerables_Are_Equivalent()
    {
        List<int> list = [1, 2, 3, 4, 5];
        
        int[] array = [1, 2, 3, 4, 5];

        await TUnitAssert.That(list).IsEquivalentTo(array);
    }
    
    [Test]
    public async Task Different_Enumerables_Are_Equivalent2()
    {
        List<int> list = [1, 2, 3, 4, 5];
        
        int[] array = [1, 2, 3, 4, 5];

        await TUnitAssert.That(array).IsEquivalentTo(list);
    }
    
    [Test]
    public async Task Different_Dictionaries_Are_Equivalent_With_Different_Ordered_Keys()
    {
        var dict1 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "A", "A" },
            { "B", "B" },
        };

        var dict2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "B", "B" },
            { "A", "A" },
        };

        await TUnitAssert.That(dict1).IsEquivalentTo(dict2);
    }
    
    [Test]
    public async Task Different_Enumerables_Are_Equivalent_Any_Order()
    {
        List<int> list = [1, 2, 3, 4, 5];
        
        int[] array = [1, 5, 2, 3, 4];

        await TUnitAssert.That(list).IsEquivalentTo(array, CollectionOrdering.Any);
    }
    
    [Test]
    public async Task Different_Enumerables_Are_Equivalent_Any_Order2()
    {
        List<int> list = [1, 2, 3, 4, 5];
        
        int[] array = [1, 5, 2, 3, 4];

        await TUnitAssert.That(array).IsEquivalentTo(list, CollectionOrdering.Any);
    }
    
    [Test]
    public async Task Different_Enumerables__Thrown_When_Non_Matching_Order()
    {
        List<int> list = [1, 2, 3, 4, 5];
        
        int[] array = [1, 5, 2, 3, 4];

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(array).IsEquivalentTo(list, CollectionOrdering.Matching));
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            """
            Expected array to be equivalent to [1, 2, 3, 4, 5]
            
            but it is [1, 5, 2, 3, 4]
            
            at Assert.That(array).IsEquivalentTo(list, CollectionOrdering.Matching)
            """
        );    
    }
    
    [Test]
    public async Task Different_Enumerables__Thrown_When_Non_Matching_Order2()
    {
        List<int> list = [1, 2, 3, 4, 5];
        
        int[] array = [1, 5, 2, 3, 4];

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(array).IsEquivalentTo(list, CollectionOrdering.Matching));
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            """
            Expected array to be equivalent to [1, 2, 3, 4, 5]
            
            but it is [1, 5, 2, 3, 4]
            
            at Assert.That(array).IsEquivalentTo(list, CollectionOrdering.Matching)
            """
        );
    }
    
    [Test]
    public async Task Different_Mismatched_Objects_Still_Are_Not_Equivalent()
    {
        var result1 = new MyClass
        {
            Value = "Foo"
        };
        
        var result2 = new { Value = "Foo1" };

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(result1).IsEquivalentTo(result2));
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            """
            Expected result1 to be equivalent to result2
            
            but Property MyClass.Value did not match
            Expected: "Foo1"
            Received: "Foo"
            
            at Assert.That(result1).IsEquivalentTo(result2)
            """
        );
    }
    
    [Test]
    public async Task Mismatched_Objects_Are_Not_Equivalent()
    {
        var object1 = new MyClass();
        var object2 = new MyClass
        {
            Value = "Foo"
        };

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(object1).IsEquivalentTo(object2));
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            """
            Expected object1 to be equivalent to object2
            
            but Property MyClass.Value did not match
            Expected: "Foo"
            Received: null
            
            at Assert.That(object1).IsEquivalentTo(object2)
            """
            );
    }
    
    [Test]
    public async Task Objects_With_Nested_Mismatch_Are_Not_Equivalent()
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

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(object1).IsEquivalentTo(object2));
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            """
            Expected object1 to be equivalent to object2
            
            but Property MyClass.Inner.Inner.Value did not match
            Expected: "Baz"
            Received: null
            
            at Assert.That(object1).IsEquivalentTo(object2)
            """
        );
    }
    
    [Test]
    public async Task Objects_With_Nested_Mismatch_Are_Not_Equivalent2()
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

        await TUnitAssert.That(object1).IsNotEquivalentTo(object2);
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
    public async Task Struct_Objects_With_Nested_Matches_Are_Equivalent()
    {
        var object1 = new MyStruct
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
        var object2 = new MyStruct
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
    public async Task Objects_With_Nested_Enumerable_Mismatch_Are_Not_Equivalent()
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

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(object1).IsEquivalentTo(object2));
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            """
            Expected object1 to be equivalent to object2
            
            but MyClass.Inner.Inner.Collection.[3] did not match
            Expected: "4"
            Received: null
            
            at Assert.That(object1).IsEquivalentTo(object2)
            """
        );
    }
    
    [Test]
    public async Task Objects_With_Nested_Enumerable_Mismatch_Are_Not_Equivalent2()
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

        await TUnitAssert.That(object1).IsNotEquivalentTo(object2);
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
    
    [Test]
    public async Task Objects_With_Partial_Properties_Match_With_Full_Equivalency_Are_Not_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar"
            }
        };
        var object2 = new
        {
            Value = "Foo",
        };
        

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.That(object1)
                                         .IsEquivalentTo(object2));
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
                             """
                             Expected object1 to be equivalent to object2

                             but Property MyClass.Inner did not match
                             Expected: null
                             Received: TUnit.Assertions.Tests.Old.EquivalentAssertionTests+InnerClass

                             at Assert.That(object1).IsEquivalentTo(object2)
                             """
                         );
    }
    
    [Test]
    public async Task Objects_With_Partial_Properties_Match_With_Full_Equivalency_Are_Not_Equivalent2()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar"
            }
        };
        var object2 = new
        {
            Value = "Foo",
        };
        

        await TUnitAssert.That(object1).IsNotEquivalentTo(object2);
    }
    
    [Test]
    public async Task Objects_With_Partial_Properties_Match_With_Partial_Equivalency_Are_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar"
            }
        };
        var object2 = new
        {
            Value = "Foo",
        };
        

        await TUnitAssert.That(object1)
                         .IsEquivalentTo(object2)
                         .WithPartialEquivalency();
    }
    
    [Test]
    public async Task Objects_With_Mismatch_With_Partial_Equivalency_Kind_Are_Not_Equivalent()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar"
            }
        };
        var object2 = new
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Baz",
            }
        };
        
        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.That(object1)
                                         .IsEquivalentTo(object2)
                                         .WithPartialEquivalency());
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
                             """
                             Expected object1 to be equivalent to object2

                             but Property MyClass.Inner.Value did not match
                             Expected: "Baz"
                             Received: "Bar"

                             at Assert.That(object1).IsEquivalentTo(object2)
                             """
                         );
    }

    [Test]
    public async Task Objects_With_Mismatch_With_Partial_Equivalency_Kind_Are_Not_Equivalent2()
    {
        var object1 = new MyClass
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Bar"
            }
        };
        var object2 = new
        {
            Value = "Foo",
            Inner = new InnerClass
            {
                Value = "Baz",
            }
        };
        
        await TUnitAssert.That(object1)
                .IsNotEquivalentTo(object2)
                .WithPartialEquivalency();
    }
    
    [Test]
    public async Task Object_With_Partial_Fields_Match_With_Full_Equivalency_Are_Not_Equivalent()
    {
        var object1 = new MyClassWithMultipleFields
        {
            value = "Foo",
            intValue = 10
        };
        var object2 = new MyClassWithSingleField
        {
            value = "Foo",
        };
        

        var exception = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(
            async () => await TUnitAssert.That(object1)
                .IsEquivalentTo(object2));
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            """
            Expected object1 to be equivalent to object2

            but Field MyClassWithMultipleFields.intValue did not match
            Expected: null
            Received: 10

            at Assert.That(object1).IsEquivalentTo(object2)
            """
        );
    }

    [Test]
    public async Task Object_With_Partial_Fields_Match_With_Full_Equivalency_Are_Not_Equivalent2()
    {
        var object1 = new MyClassWithMultipleFields
        {
            value = "Foo",
            intValue = 10
        };
        var object2 = new MyClassWithSingleField
        {
            value = "Foo",
        };
        

        await TUnitAssert.That(object1).IsNotEquivalentTo(object2);
    }

    [Test]
    public async Task Object_With_Partial_Fields_Match_With_Partial_Equivalency_Are_Equivalent()
    {
        var object1 = new MyClassWithMultipleFields
        {
            value = "Foo",
            intValue = 10
        };
        var object2 = new MyClassWithSingleField
        {
            value = "Foo",
        };
        

        await TUnitAssert.That(object1)
                         .IsEquivalentTo(object2)
                         .WithPartialEquivalency();
    }

    
    public class MyClassWithMultipleFields
    {
        public string? value;
        public int intValue;
    }

    public class MyClassWithSingleField
    {
        public string? value;
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
    
    public struct MyStruct
    {
        public string? Value { get; set; }
        public InnerClass? Inner { get; set; }
    }
}