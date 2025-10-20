using System.Text;

namespace TUnit.Assertions.Tests;

public class TypeOfTests
{
    // Test struct for custom struct testing
    private struct CustomStruct
    {
        public int Value { get; init; }
        public string Name { get; init; }
    }

    [Test]
    public async Task Returns_Casted_Object()
    {
        object? obj = new StringBuilder();

        var result = await Assert.That(obj).IsTypeOf<StringBuilder>();

        await Assert.That(result).IsNotNull();
    }

    // ============ STRUCT / VALUE TYPE TESTS ============

    [Test]
    public async Task IsTypeOf_BoxedInt_Success()
    {
        object boxedInt = 42;

        var result = await Assert.That(boxedInt).IsTypeOf<int>();

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task IsTypeOf_BoxedDateTime_Success()
    {
        var expectedDate = new DateTime(2025, 10, 14);
        object boxedDateTime = expectedDate;

        var result = await Assert.That(boxedDateTime).IsTypeOf<DateTime>();

        await Assert.That(result).IsEqualTo(expectedDate);
    }

    [Test]
    public async Task IsTypeOf_BoxedGuid_Success()
    {
        var expectedGuid = Guid.NewGuid();
        object boxedGuid = expectedGuid;

        var result = await Assert.That(boxedGuid).IsTypeOf<Guid>();

        await Assert.That(result).IsEqualTo(expectedGuid);
    }

    [Test]
    public async Task IsTypeOf_CustomStruct_Success()
    {
        var customStruct = new CustomStruct { Value = 10, Name = "Test" };
        object boxedCustom = customStruct;

        var result = await Assert.That(boxedCustom).IsTypeOf<CustomStruct>();

        await Assert.That(result.Value).IsEqualTo(10);
        await Assert.That(result.Name).IsEqualTo("Test");
    }

    [Test]
    public async Task IsTypeOf_NullableInt_WithTwoTypeParameters_Success()
    {
        int? nullableInt = 42;

        var result = await Assert.That(nullableInt).IsTypeOf<int?, int?>();

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task IsTypeOf_BoxedNullableInt_Success()
    {
        object? boxedNullableInt = (int?)42;

        var result = await Assert.That(boxedNullableInt).IsTypeOf<int?>();

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task IsTypeOf_StructTypeMismatch_Fails()
    {
        object boxedInt = 42;

        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(boxedInt).IsTypeOf<long>();
        });
    }

    [Test]
    public async Task IsTypeOf_DecimalStruct_Success()
    {
        object boxedDecimal = 123.45m;

        var result = await Assert.That(boxedDecimal).IsTypeOf<decimal>();

        await Assert.That(result).IsEqualTo(123.45m);
    }

    [Test]
    public async Task IsTypeOf_TimeSpanStruct_Success()
    {
        var expectedTimeSpan = TimeSpan.FromHours(2);
        object boxedTimeSpan = expectedTimeSpan;

        var result = await Assert.That(boxedTimeSpan).IsTypeOf<TimeSpan>();

        await Assert.That(result).IsEqualTo(expectedTimeSpan);
    }

    // ============ UNBOXED VALUE TYPE TESTS ============

    [Test]
    public async Task IsTypeOf_UnboxedInt_Success()
    {
        int number = 42;

        // Using two type parameters - should work with unboxed value types
        var result = await Assert.That(number).IsTypeOf<int, int>();

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task IsTypeOf_UnboxedDateTime_Success()
    {
        var date = new DateTime(2025, 10, 14, 12, 30, 0);

        var result = await Assert.That(date).IsTypeOf<DateTime, DateTime>();

        await Assert.That(result).IsEqualTo(date);
    }

    [Test]
    public async Task IsTypeOf_UnboxedGuid_Success()
    {
        var guid = Guid.NewGuid();

        var result = await Assert.That(guid).IsTypeOf<Guid, Guid>();

        await Assert.That(result).IsEqualTo(guid);
    }

    [Test]
    public async Task IsTypeOf_UnboxedBool_Success()
    {
        bool value = true;

        var result = await Assert.That(value).IsTypeOf<bool, bool>();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsTypeOf_UnboxedDouble_Success()
    {
        double value = 3.14159;

        var result = await Assert.That(value).IsTypeOf<double, double>();

        await Assert.That(result).IsEqualTo(3.14159);
    }

    [Test]
    public async Task IsTypeOf_UnboxedCustomStruct_Success()
    {
        var customStruct = new CustomStruct { Value = 99, Name = "Unboxed" };

        var result = await Assert.That(customStruct).IsTypeOf<CustomStruct, CustomStruct>();

        await Assert.That(result.Value).IsEqualTo(99);
        await Assert.That(result.Name).IsEqualTo("Unboxed");
    }

    // ============ NULLABLE COLLECTION TESTS ============

    [Test]
    public async Task IsNotNull_NullableByteArray_ReturnsNonNull()
    {
        byte[]? nullableBytes = new byte[] { 1, 2, 3 };

        // IsNotNull should succeed and return non-null collection
        var result = await Assert.That(nullableBytes).IsNotNull();

        // Result should be enumerable with 3 elements
        await Assert.That(result.Count()).IsEqualTo(3);
        await Assert.That(result.First()).IsEqualTo((byte)1);
    }

    [Test]
    public async Task IsNotNull_NullableIntArray_ReturnsNonNull()
    {
        int[]? nullableArray = new int[] { 10, 20, 30 };

        // IsNotNull should succeed and return non-null collection
        var result = await Assert.That(nullableArray).IsNotNull();

        await Assert.That(result.Count()).IsEqualTo(3);
        await Assert.That(result.ElementAt(1)).IsEqualTo(20);
    }

    [Test]
    public async Task IsNotNull_NullableList_ReturnsNonNull()
    {
        List<string>? nullableList = new List<string> { "a", "b", "c" };

        // IsNotNull should succeed and return non-null collection
        var result = await Assert.That(nullableList).IsNotNull();

        // Result should be enumerable with 3 elements
        await Assert.That(result.Count()).IsEqualTo(3);
        await Assert.That(result.First()).IsEqualTo("a");
    }

    [Test]
    public async Task IsNotNull_NullableIEnumerable_ReturnsIEnumerable()
    {
        IEnumerable<int>? nullableEnumerable = Enumerable.Range(1, 5);

        var result = await Assert.That(nullableEnumerable).IsNotNull();

        // Result should be enumerable
        await Assert.That(result.Count()).IsEqualTo(5);
        await Assert.That(result.First()).IsEqualTo(1);
    }

    [Test]
    public async Task CollectionAssertion_NullableByteArray_CanUseCollectionMethods()
    {
        byte[]? nullableBytes = new byte[] { 1, 2, 3, 4, 5 };

        // Should be able to use collection assertion methods
        await Assert.That(nullableBytes).HasCount(5);
        await Assert.That(nullableBytes).Contains((byte)3);
        await Assert.That(nullableBytes).IsInOrder();

    }

    [Test]
    public async Task CollectionAssertion_NullableList_CanUseCollectionMethods()
    {
        List<int>? nullableList = new List<int> { 10, 20, 30 };

        await Assert.That(nullableList).HasCount(3);
        await Assert.That(nullableList).Contains(20);
        await Assert.That(nullableList).IsInOrder();
    }

    [Test]
    public async Task CollectionAssertion_NullableIEnumerable_CanUseCollectionMethods()
    {
        IEnumerable<string>? nullableEnumerable = new[] { "apple", "banana", "cherry" };

        await Assert.That(nullableEnumerable).HasCount(3);
        await Assert.That(nullableEnumerable).Contains("banana");
        await Assert.That(nullableEnumerable).IsInOrder();
    }

    // ============ REFERENCE TYPE TESTS (existing behavior verification) ============

    [Test]
    public async Task IsTypeOf_String_Success()
    {
        object obj = "Hello";

        var result = await Assert.That(obj).IsTypeOf<string>();

        await Assert.That(result).IsEqualTo("Hello");
    }

    [Test]
    public async Task IsTypeOf_StringBuilder_Success()
    {
        object obj = new StringBuilder("Test");

        var result = await Assert.That(obj).IsTypeOf<StringBuilder>();

        await Assert.That(result.ToString()).IsEqualTo("Test");
    }

    [Test]
    public async Task IsTypeOf_ReferenceTypeMismatch_Fails()
    {
        object obj = "Hello";

        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(obj).IsTypeOf<StringBuilder>();
        });
    }

    // ============ CONCRETE TYPE TESTS (Issue #3391) ============

    [Test]
    public async Task IsTypeOf_ConcreteList_AsObject_SingleTypeParameter()
    {
        // The correct usage: have an object-typed variable and check its type
        object list = new List<string> { "a", "b", "c" };

        // This works with single type parameter when source is object
        var result = await Assert.That(list).IsTypeOf<List<string>>();

        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_BaseType_CheckDerivedType()
    {
        // Real-world scenario: have a base type and want to assert/cast to derived type
        IEnumerable<string> enumerable = new List<string> { "a", "b", "c" };

        // Check if it's actually a List<string>
        var result = await Assert.That(enumerable).IsTypeOf<List<string>, IEnumerable<string>>();

        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_ConcreteList_AfterCastToObject()
    {
        var list = new List<string> { "a", "b", "c" };

        // This works by casting to object first (workaround for strongly-typed variables)
        var result = await Assert.That((object)list).IsTypeOf<List<string>>();

        await Assert.That(result.Count).IsEqualTo(3);
    }

    // ============ INTERFACE SOURCE TYPE TESTS ============

    [Test]
    public async Task IsTypeOf_IListToList_Success()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        // Now works with single type parameter!
        var result = await Assert.That(list).IsTypeOf<List<int>>();

        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.Capacity).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_ICollectionToList_Success()
    {
        ICollection<string> collection = new List<string> { "x", "y", "z" };

        var result = await Assert.That(collection).IsTypeOf<List<string>>();

        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_IEnumerableToArray_Success()
    {
        IEnumerable<int> enumerable = new int[] { 10, 20, 30 };

        var result = await Assert.That(enumerable).IsTypeOf<int[], IEnumerable<int>>();

        await Assert.That(result.Length).IsEqualTo(3);
        await Assert.That(result[1]).IsEqualTo(20);
    }

    [Test]
    public async Task IsTypeOf_IReadOnlyListToList_Success()
    {
        IReadOnlyList<double> readOnlyList = new List<double> { 1.1, 2.2, 3.3 };

        var result = await Assert.That(readOnlyList).IsTypeOf<List<double>>();

        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_IDictionaryToDictionary_Success()
    {
        IDictionary<string, int> dict = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2
        };

        var result = await Assert.That(dict).IsTypeOf<Dictionary<string, int>>();

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result["a"]).IsEqualTo(1);
    }

    [Test]
    public async Task IsTypeOf_IEnumerableToHashSet_Success()
    {
        IEnumerable<string> enumerable = new HashSet<string> { "alpha", "beta", "gamma" };

        var result = await Assert.That(enumerable).IsTypeOf<HashSet<string>, IEnumerable<string>>();

        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.Contains("beta")).IsTrue();
    }

    // ============ ARRAY TESTS ============

    [Test]
    public async Task IsTypeOf_ObjectToStringArray_Success()
    {
        object arr = new string[] { "hello", "world" };

        var result = await Assert.That(arr).IsTypeOf<string[]>();

        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0]).IsEqualTo("hello");
    }

    [Test]
    public async Task IsTypeOf_ObjectToIntArray_Success()
    {
        object arr = new int[] { 5, 10, 15 };

        var result = await Assert.That(arr).IsTypeOf<int[]>();

        await Assert.That(result.Length).IsEqualTo(3);
        await Assert.That(result[2]).IsEqualTo(15);
    }

    [Test]
    public async Task IsTypeOf_WrongArrayType_Fails()
    {
        object arr = new string[] { "a", "b" };

        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(arr).IsTypeOf<int[]>();
        });
    }

    // ============ INHERITANCE HIERARCHY TESTS ============

    private abstract class Animal
    {
        public abstract string MakeSound();
    }

    private class Dog : Animal
    {
        public override string MakeSound() => "Woof";
        public string Breed { get; set; } = "Unknown";
    }

    private class Cat : Animal
    {
        public override string MakeSound() => "Meow";
        public bool IsIndoor { get; set; } = true;
    }

    [Test]
    public async Task IsTypeOf_AbstractBaseToConcreteClass_Success()
    {
        Animal animal = new Dog { Breed = "Labrador" };

        var result = await Assert.That(animal).IsTypeOf<Dog>();

        await Assert.That(result.MakeSound()).IsEqualTo("Woof");
        await Assert.That(result.Breed).IsEqualTo("Labrador");
    }

    [Test]
    public async Task IsTypeOf_AbstractBaseToWrongConcreteClass_Fails()
    {
        Animal animal = new Dog();

        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(animal).IsTypeOf<Cat>();
        });
    }

    [Test]
    public async Task IsTypeOf_ObjectToAbstractClass_Success()
    {
        object obj = new Dog();

        // Can assert to abstract type when actual object is derived from it
        var result = await Assert.That(obj).IsTypeOf<Animal>();

        await Assert.That(result.MakeSound()).IsEqualTo("Woof");
    }

    // ============ GENERIC TYPE TESTS ============

    [Test]
    public async Task IsTypeOf_ListOfInt_Success()
    {
        object obj = new List<int> { 1, 2, 3 };

        var result = await Assert.That(obj).IsTypeOf<List<int>>();

        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_ListOfInt_WrongGenericType_Fails()
    {
        object obj = new List<int> { 1, 2, 3 };

        // List<int> is not List<string>
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(obj).IsTypeOf<List<string>>();
        });
    }

    [Test]
    public async Task IsTypeOf_DictionaryWithGenericTypes_Success()
    {
        object obj = new Dictionary<string, List<int>>
        {
            ["numbers"] = new List<int> { 1, 2, 3 }
        };

        var result = await Assert.That(obj).IsTypeOf<Dictionary<string, List<int>>>();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result["numbers"].Count).IsEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_NestedGenericTypes_Success()
    {
        object obj = new List<List<string>>
        {
            new List<string> { "a", "b" },
            new List<string> { "c", "d" }
        };

        var result = await Assert.That(obj).IsTypeOf<List<List<string>>>();

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Count).IsEqualTo(2);
    }

    // ============ TUPLE TESTS ============

    [Test]
    public async Task IsTypeOf_ValueTuple_Success()
    {
        object obj = (42, "answer");

        var result = await Assert.That(obj).IsTypeOf<(int, string)>();

        await Assert.That(result.Item1).IsEqualTo(42);
        await Assert.That(result.Item2).IsEqualTo("answer");
    }

    [Test]
    public async Task IsTypeOf_ValueTupleWrongTypes_Fails()
    {
        object obj = (42, "answer");

        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(obj).IsTypeOf<(string, int)>();
        });
    }

    [Test]
    public async Task IsTypeOf_TupleClass_Success()
    {
        object obj = Tuple.Create(100, "test", true);

        var result = await Assert.That(obj).IsTypeOf<Tuple<int, string, bool>>();

        await Assert.That(result.Item1).IsEqualTo(100);
        await Assert.That(result.Item2).IsEqualTo("test");
        await Assert.That(result.Item3).IsTrue();
    }

    // ============ LINQ QUERY TESTS ============

    [Test]
    public async Task IsTypeOf_LinqQueryToEnumerable_Success()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var query = numbers.Where(n => n > 2);  // Returns IEnumerable<int>

        // The actual type is a LINQ iterator
        object queryAsObject = query;

        // We can check it's IEnumerable<int>
        var result = await Assert.That(queryAsObject).IsTypeOf<IEnumerable<int>>();

        await Assert.That(result.Count()).IsEqualTo(3);
    }

    // ============ COLLECTION IMPLEMENTATIONS TESTS ============

    [Test]
    public async Task IsTypeOf_LinkedListFromIEnumerable_Success()
    {
        IEnumerable<int> enumerable = new LinkedList<int>(new[] { 10, 20, 30 });

        var result = await Assert.That(enumerable).IsTypeOf<LinkedList<int>, IEnumerable<int>>();

        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.First!.Value).IsEqualTo(10);
    }

    [Test]
    public async Task IsTypeOf_QueueFromObject_Success()
    {
        object obj = new Queue<string>(new[] { "first", "second", "third" });

        var result = await Assert.That(obj).IsTypeOf<Queue<string>>();

        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.Peek()).IsEqualTo("first");
    }

    [Test]
    public async Task IsTypeOf_StackFromObject_Success()
    {
        object obj = new Stack<int>(new[] { 1, 2, 3 });

        var result = await Assert.That(obj).IsTypeOf<Stack<int>>();

        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_SortedSetFromISet_Success()
    {
        ISet<string> set = new SortedSet<string> { "charlie", "alice", "bob" };

        var result = await Assert.That(set).IsTypeOf<SortedSet<string>>();

        await Assert.That(result.Count).IsEqualTo(3);
        // SortedSet maintains order
        await Assert.That(result.First()).IsEqualTo("alice");
    }

    // ============ NULL AND EDGE CASES ============

    [Test]
    public async Task IsTypeOf_NullObject_Fails()
    {
        object? obj = null;

        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(obj).IsTypeOf<string>();
        });
    }

    [Test]
    public async Task IsTypeOf_EmptyList_Success()
    {
        object obj = new List<int>();

        var result = await Assert.That(obj).IsTypeOf<List<int>>();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task IsTypeOf_EmptyArray_Success()
    {
        object obj = Array.Empty<string>();

        var result = await Assert.That(obj).IsTypeOf<string[]>();

        await Assert.That(result.Length).IsEqualTo(0);
    }
}
