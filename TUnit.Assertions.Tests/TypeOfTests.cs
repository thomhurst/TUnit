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
}
