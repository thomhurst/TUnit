namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for Member() and HasProperty() with various nullable and non-nullable type combinations.
/// Ensures the TMember? fix for nullable selectors works correctly for:
/// - Non-nullable reference types (string, object, custom class)
/// - Nullable reference types (string?, object?, custom class?)
/// - Non-nullable value types / structs (int, bool, DateTime, custom struct)
/// - Nullable value types / structs (int?, bool?, DateTime?, custom struct?)
/// - Enum types (nullable and non-nullable)
/// - Collection types (nullable and non-nullable)
/// </summary>
public class MemberNullabilityTests
{
    // ============ MODEL CLASSES ============

    private sealed class ModelWithAllTypes
    {
        // Non-nullable reference types
        public string Name { get; set; } = string.Empty;
        public object Tag { get; set; } = new();
        public InnerModel Inner { get; set; } = new();
        public List<int> Numbers { get; set; } = [];

        // Nullable reference types
        public string? NullableName { get; set; }
        public object? NullableTag { get; set; }
        public InnerModel? NullableInner { get; set; }
        public List<int>? NullableNumbers { get; set; }

        // Non-nullable value types (structs)
        public int Count { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public MyStruct StructValue { get; set; }
        public MyEnum EnumValue { get; set; }

        // Nullable value types (structs)
        public int? NullableCount { get; set; }
        public bool? NullableFlag { get; set; }
        public DateTime? NullableDate { get; set; }
        public MyStruct? NullableStructValue { get; set; }
        public MyEnum? NullableEnumValue { get; set; }
    }

    private sealed class InnerModel
    {
        public string Value { get; set; } = "default";
    }

    private struct MyStruct
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private enum MyEnum
    {
        None = 0,
        First = 1,
        Second = 2
    }

    // ============ NON-NULLABLE REFERENCE TYPE TESTS ============

    [Test]
    public async Task Member_NonNullableString_Success()
    {
        var model = new ModelWithAllTypes { Name = "hello" };
        await Assert.That(model).Member(x => x.Name, name => name.IsEqualTo("hello"));
    }

    [Test]
    public async Task Member_NonNullableString_Failure()
    {
        var model = new ModelWithAllTypes { Name = "hello" };
        var ex = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(model).Member(x => x.Name, name => name.IsEqualTo("world")));
        await Assert.That(ex!.Message).Contains("world");
    }

    [Test]
    public async Task Member_NonNullableObject_Success()
    {
        var model = new ModelWithAllTypes { Tag = "tag-value" };
        await Assert.That(model).Member(x => x.Tag, tag => tag.IsNotNull());
    }

    [Test]
    public async Task Member_NonNullableInnerModel_Success()
    {
        var model = new ModelWithAllTypes { Inner = new InnerModel { Value = "test" } };
        await Assert.That(model).Member(x => x.Inner, inner => inner.IsNotNull());
    }

    [Test]
    public async Task Member_NonNullableCollection_Success()
    {
        var model = new ModelWithAllTypes { Numbers = [1, 2, 3] };
        await Assert.That(model).Member(x => x.Numbers, nums => nums.IsNotNull());
    }

    // ============ NULLABLE REFERENCE TYPE TESTS ============

    [Test]
    public async Task Member_NullableString_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableName = "dbo" };
        await Assert.That(model).Member(x => x.NullableName, name => name.IsEqualTo("dbo"));
    }

    [Test]
    public async Task Member_NullableString_WithNull_IsNull_Success()
    {
        var model = new ModelWithAllTypes { NullableName = null };
        await Assert.That(model).Member(x => x.NullableName, name => name.IsNull());
    }

    [Test]
    public async Task Member_NullableString_WithValue_IsNotNull_Success()
    {
        var model = new ModelWithAllTypes { NullableName = "test" };
        await Assert.That(model).Member(x => x.NullableName, name => name.IsNotNull());
    }

    [Test]
    public async Task Member_NullableObject_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableTag = "tag" };
        await Assert.That(model).Member(x => x.NullableTag, tag => tag.IsNotNull());
    }

    [Test]
    public async Task Member_NullableObject_WithNull_Success()
    {
        var model = new ModelWithAllTypes { NullableTag = null };
        await Assert.That(model).Member(x => x.NullableTag, tag => tag.IsNull());
    }

    [Test]
    public async Task Member_NullableInnerModel_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableInner = new InnerModel { Value = "x" } };
        await Assert.That(model).Member(x => x.NullableInner, inner => inner.IsNotNull());
    }

    [Test]
    public async Task Member_NullableInnerModel_WithNull_Success()
    {
        var model = new ModelWithAllTypes { NullableInner = null };
        await Assert.That(model).Member(x => x.NullableInner, inner => inner.IsNull());
    }

    [Test]
    public async Task Member_NullableCollection_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableNumbers = [10, 20] };
        await Assert.That(model).Member(x => x.NullableNumbers, nums => nums.IsNotNull());
    }

    [Test]
    public async Task Member_NullableCollection_WithNull_Success()
    {
        var model = new ModelWithAllTypes { NullableNumbers = null };
        await Assert.That(model).Member(x => x.NullableNumbers, nums => nums.IsNull());
    }

    // ============ NON-NULLABLE VALUE TYPE (STRUCT) TESTS ============

    [Test]
    public async Task Member_NonNullableInt_Success()
    {
        var model = new ModelWithAllTypes { Count = 42 };
        await Assert.That(model).Member(x => x.Count, count => count.IsEqualTo(42));
    }

    [Test]
    public async Task Member_NonNullableInt_Failure()
    {
        var model = new ModelWithAllTypes { Count = 42 };
        var ex = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(model).Member(x => x.Count, count => count.IsEqualTo(99)));
        await Assert.That(ex!.Message).Contains("99");
    }

    [Test]
    public async Task Member_NonNullableBool_Success()
    {
        var model = new ModelWithAllTypes { IsActive = true };
        await Assert.That(model).Member(x => x.IsActive, active => active.IsTrue());
    }

    [Test]
    public async Task Member_NonNullableDateTime_Success()
    {
        var date = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var model = new ModelWithAllTypes { CreatedAt = date };
        await Assert.That(model).Member(x => x.CreatedAt, d => d.IsEqualTo(date));
    }

    [Test]
    public async Task Member_NonNullableCustomStruct_Success()
    {
        var expected = new MyStruct { X = 1, Y = 2 };
        var model = new ModelWithAllTypes { StructValue = expected };
        await Assert.That(model).Member(x => x.StructValue, s => s.IsEqualTo(expected));
    }

    [Test]
    public async Task Member_NonNullableEnum_Success()
    {
        var model = new ModelWithAllTypes { EnumValue = MyEnum.First };
        await Assert.That(model).Member(x => x.EnumValue, e => e.IsEqualTo(MyEnum.First));
    }

    // ============ NULLABLE VALUE TYPE (STRUCT) TESTS ============

    [Test]
    public async Task Member_NullableInt_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableCount = 10 };
        await Assert.That(model).Member(x => x.NullableCount, count => count.IsEqualTo(10));
    }

    [Test]
    public async Task Member_NullableInt_WithNull_IsNull_Success()
    {
        var model = new ModelWithAllTypes { NullableCount = null };
        await Assert.That(model).Member(x => x.NullableCount, count => count.IsNull());
    }

    [Test]
    public async Task Member_NullableInt_WithValue_IsNotNull_Success()
    {
        var model = new ModelWithAllTypes { NullableCount = 5 };
        await Assert.That(model).Member(x => x.NullableCount, count => count.IsNotNull());
    }

    [Test]
    public async Task Member_NullableBool_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableFlag = true };
        await Assert.That(model).Member(x => x.NullableFlag, flag => flag.IsEqualTo(true));
    }

    [Test]
    public async Task Member_NullableBool_WithNull_IsNull_Success()
    {
        var model = new ModelWithAllTypes { NullableFlag = null };
        await Assert.That(model).Member(x => x.NullableFlag, flag => flag.IsNull());
    }

    [Test]
    public async Task Member_NullableDateTime_WithValue_Success()
    {
        var date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var model = new ModelWithAllTypes { NullableDate = date };
        await Assert.That(model).Member(x => x.NullableDate, d => d.IsEqualTo(date));
    }

    [Test]
    public async Task Member_NullableDateTime_WithNull_IsNull_Success()
    {
        var model = new ModelWithAllTypes { NullableDate = null };
        await Assert.That(model).Member(x => x.NullableDate, d => d.IsNull());
    }

    [Test]
    public async Task Member_NullableCustomStruct_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableStructValue = new MyStruct { X = 3, Y = 4 } };
        await Assert.That(model).Member(x => x.NullableStructValue, s => s.IsNotNull());
    }

    [Test]
    public async Task Member_NullableCustomStruct_WithNull_IsNull_Success()
    {
        var model = new ModelWithAllTypes { NullableStructValue = null };
        await Assert.That(model).Member(x => x.NullableStructValue, s => s.IsNull());
    }

    [Test]
    public async Task Member_NullableEnum_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableEnumValue = MyEnum.Second };
        await Assert.That(model).Member(x => x.NullableEnumValue, e => e.IsEqualTo(MyEnum.Second));
    }

    [Test]
    public async Task Member_NullableEnum_WithNull_IsNull_Success()
    {
        var model = new ModelWithAllTypes { NullableEnumValue = null };
        await Assert.That(model).Member(x => x.NullableEnumValue, e => e.IsNull());
    }

    // ============ CHAINING TESTS (mixed types) ============

    [Test]
    public async Task Member_Chained_MixedNullableAndNonNullable()
    {
        var model = new ModelWithAllTypes
        {
            Name = "Test",
            NullableName = "Schema",
            Count = 5,
            NullableCount = 10
        };

        await Assert.That(model)
            .Member(x => x.Name, name => name.IsEqualTo("Test"))
            .And.Member(x => x.NullableName, name => name.IsEqualTo("Schema"))
            .And.Member(x => x.Count, count => count.IsEqualTo(5))
            .And.Member(x => x.NullableCount, count => count.IsEqualTo(10));
    }

    [Test]
    public async Task Member_Chained_NullableRefAndStruct()
    {
        var model = new ModelWithAllTypes
        {
            NullableName = null,
            NullableCount = null
        };

        await Assert.That(model)
            .Member(x => x.NullableName, name => name.IsNull())
            .And.Member(x => x.NullableCount, count => count.IsNull());
    }

    // ============ HasProperty TESTS ============

    [Test]
    public async Task HasProperty_NonNullableString_Success()
    {
        var model = new ModelWithAllTypes { Name = "hello" };
        await Assert.That(model).HasProperty(x => x.Name, "hello");
    }

    [Test]
    public async Task HasProperty_NullableString_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableName = "dbo" };
        await Assert.That(model).HasProperty(x => x.NullableName, "dbo");
    }

    [Test]
    public async Task HasProperty_NonNullableInt_Success()
    {
        var model = new ModelWithAllTypes { Count = 42 };
        await Assert.That(model).HasProperty(x => x.Count, 42);
    }

    [Test]
    public async Task HasProperty_NullableInt_WithValue_Success()
    {
        var model = new ModelWithAllTypes { NullableCount = 7 };
        await Assert.That(model).HasProperty(x => x.NullableCount, 7);
    }

    [Test]
    public async Task HasProperty_NonNullableBool_Success()
    {
        var model = new ModelWithAllTypes { IsActive = true };
        await Assert.That(model).HasProperty(x => x.IsActive, true);
    }

    [Test]
    public async Task HasProperty_NonNullableEnum_Success()
    {
        var model = new ModelWithAllTypes { EnumValue = MyEnum.First };
        await Assert.That(model).HasProperty(x => x.EnumValue, MyEnum.First);
    }

    [Test]
    public async Task HasProperty_Fluent_NullableString_IsNull()
    {
        var model = new ModelWithAllTypes { NullableName = null };
        await Assert.That(model).HasProperty(x => x.NullableName).IsNull();
    }

    [Test]
    public async Task HasProperty_Fluent_NullableString_IsNotNull()
    {
        var model = new ModelWithAllTypes { NullableName = "value" };
        await Assert.That(model).HasProperty(x => x.NullableName).IsNotNull();
    }

    [Test]
    public async Task HasProperty_Fluent_NullableInt_IsNull()
    {
        var model = new ModelWithAllTypes { NullableCount = null };
        await Assert.That(model).HasProperty(x => x.NullableCount).IsNull();
    }

    [Test]
    public async Task HasProperty_Fluent_NullableInt_IsNotNull()
    {
        var model = new ModelWithAllTypes { NullableCount = 42 };
        await Assert.That(model).HasProperty(x => x.NullableCount).IsNotNull();
    }

    [Test]
    public async Task HasProperty_Chained_MixedTypes()
    {
        var model = new ModelWithAllTypes
        {
            Name = "Test",
            Count = 5,
            IsActive = true,
            EnumValue = MyEnum.Second
        };

        await Assert.That(model)
            .HasProperty(x => x.Name, "Test")
            .And.HasProperty(x => x.Count, 5)
            .And.HasProperty(x => x.IsActive, true)
            .And.HasProperty(x => x.EnumValue, MyEnum.Second);
    }
}
