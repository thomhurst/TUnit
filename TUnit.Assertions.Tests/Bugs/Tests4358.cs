namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Tests for issue #4358: IsEquivalentTo broken for value types
/// ValueTuples and structs containing reference type properties were incorrectly
/// compared using Equals() instead of structural comparison.
/// </summary>
public class Tests4358
{
    #region Test Types

    public record Thing(string Name, int[] Numbers);

    public record Person(string Name, int Age, List<string> Tags);

    public class NonRecordClass
    {
        public string Value { get; set; } = string.Empty;
        public int[] Data { get; set; } = [];
    }

    public struct StructWithReferenceProperties
    {
        public string Name { get; set; }
        public int[] Numbers { get; set; }
        public List<string> Tags { get; set; }
    }

    public struct StructWithEquatable : IEquatable<StructWithEquatable>
    {
        public string Id { get; set; }
        public int[] Data { get; set; }

        // This Equals only compares Id, NOT Data - demonstrating why structural comparison is needed
        public bool Equals(StructWithEquatable other) => Id == other.Id;
        public override bool Equals(object? obj) => obj is StructWithEquatable other && Equals(other);
        public override int GetHashCode() => Id?.GetHashCode() ?? 0;
    }

    public struct NestedStruct
    {
        public string Name { get; set; }
        public StructWithReferenceProperties Inner { get; set; }
    }

    public struct StructWithNullableReference
    {
        public string? Name { get; set; }
        public int[]? Numbers { get; set; }
    }

    #endregion

    #region Original Bug Reproduction Tests

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithEquivalentRecords_ShouldSucceed()
    {
        // Arrange - Two structurally equivalent records (different instances, same content)
        var foo = new Thing("Foo", [1, 2, 3]);
        var bar = new Thing("Foo", [1, 2, 3]);

        // Act & Assert - Should pass because records have equivalent content
        // This test verifies that ValueTuples use structural comparison, not Equals()
        await Assert.That((foo, bar)).IsEquivalentTo((bar, foo));
    }

    [Test]
    public async Task IsEquivalentTo_Record_WithArrayProperty_ShouldSucceed()
    {
        // Arrange - Two records with identical array content but different array instances
        var foo = new Thing("Foo", [1, 2, 3]);
        var bar = new Thing("Foo", [1, 2, 3]);

        // Act & Assert - Records with equivalent content should be considered equivalent
        await Assert.That(foo).IsEquivalentTo(bar);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithDifferentRecords_ShouldFail()
    {
        // Arrange - Two records with different content
        var foo = new Thing("Foo", [1, 2, 3]);
        var bar = new Thing("Bar", [4, 5, 6]);

        // Act & Assert - Should fail because records have different content
        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That((foo, bar)).IsEquivalentTo((bar, foo)));

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region ValueTuple Arity Tests

    [Test]
    public async Task IsEquivalentTo_ValueTuple2_WithEquivalentContent_ShouldSucceed()
    {
        var tuple1 = (new Thing("A", [1]), new Thing("B", [2]));
        var tuple2 = (new Thing("A", [1]), new Thing("B", [2]));

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple3_WithEquivalentContent_ShouldSucceed()
    {
        var tuple1 = (new Thing("A", [1]), new Thing("B", [2]), new Thing("C", [3]));
        var tuple2 = (new Thing("A", [1]), new Thing("B", [2]), new Thing("C", [3]));

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple4_WithEquivalentContent_ShouldSucceed()
    {
        var tuple1 = (new Thing("A", [1]), new Thing("B", [2]), new Thing("C", [3]), new Thing("D", [4]));
        var tuple2 = (new Thing("A", [1]), new Thing("B", [2]), new Thing("C", [3]), new Thing("D", [4]));

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple5_WithEquivalentContent_ShouldSucceed()
    {
        var tuple1 = ("a", "b", "c", "d", new int[] { 1, 2, 3 });
        var tuple2 = ("a", "b", "c", "d", new int[] { 1, 2, 3 });

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple6_WithEquivalentContent_ShouldSucceed()
    {
        var tuple1 = (1, 2, 3, 4, 5, new string[] { "a", "b" });
        var tuple2 = (1, 2, 3, 4, 5, new string[] { "a", "b" });

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple7_WithEquivalentContent_ShouldSucceed()
    {
        var tuple1 = (1, 2, 3, 4, 5, 6, new List<int> { 7, 8, 9 });
        var tuple2 = (1, 2, 3, 4, 5, 6, new List<int> { 7, 8, 9 });

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    #endregion

    #region Nested ValueTuple Tests

    [Test]
    public async Task IsEquivalentTo_NestedValueTuple_WithEquivalentContent_ShouldSucceed()
    {
        var foo = new Thing("Foo", [1, 2, 3]);
        var bar = new Thing("Bar", [4, 5, 6]);

        var tuple1 = ((foo, bar), "test");
        var tuple2 = ((new Thing("Foo", [1, 2, 3]), new Thing("Bar", [4, 5, 6])), "test");

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_DeeplyNestedValueTuple_ShouldSucceed()
    {
        var tuple1 = (((1, new int[] { 2, 3 }), "inner"), "outer");
        var tuple2 = (((1, new int[] { 2, 3 }), "inner"), "outer");

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_DeeplyNestedValueTuple_DifferentArrayContent_ShouldFail()
    {
        var tuple1 = (((1, new int[] { 2, 3 }), "inner"), "outer");
        var tuple2 = (((1, new int[] { 2, 99 }), "inner"), "outer");

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(tuple1).IsEquivalentTo(tuple2));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IsEquivalentTo_TripleNestedValueTuple_ShouldSucceed()
    {
        var innermost = (new Thing("Deep", [1, 2, 3]), 42);
        var middle = (innermost, "middle");
        var outer1 = (middle, new int[] { 10, 20 });

        var innermost2 = (new Thing("Deep", [1, 2, 3]), 42);
        var middle2 = (innermost2, "middle");
        var outer2 = (middle2, new int[] { 10, 20 });

        await Assert.That(outer1).IsEquivalentTo(outer2);
    }

    #endregion

    #region ValueTuple with Primitives Tests

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithPrimitives_ShouldSucceed()
    {
        var tuple1 = (42, "hello", 3.14);
        var tuple2 = (42, "hello", 3.14);

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithPrimitives_DifferentValues_ShouldFail()
    {
        var tuple1 = (42, "hello", 3.14);
        var tuple2 = (99, "world", 2.71);

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(tuple1).IsEquivalentTo(tuple2));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithMixedPrimitivesAndArrays_ShouldSucceed()
    {
        var tuple1 = (42, new int[] { 1, 2, 3 }, "test", 3.14);
        var tuple2 = (42, new int[] { 1, 2, 3 }, "test", 3.14);

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithDateTime_ShouldSucceed()
    {
        var date = new DateTime(2024, 1, 15, 10, 30, 0);
        var tuple1 = (date, new int[] { 1, 2, 3 });
        var tuple2 = (new DateTime(2024, 1, 15, 10, 30, 0), new int[] { 1, 2, 3 });

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithGuid_ShouldSucceed()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var tuple1 = (guid, new string[] { "a", "b" });
        var tuple2 = (Guid.Parse("12345678-1234-1234-1234-123456789012"), new string[] { "a", "b" });

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    #endregion

    #region Struct with Reference Properties Tests

    [Test]
    public async Task IsEquivalentTo_StructWithReferenceProperties_SameContent_ShouldSucceed()
    {
        var struct1 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [1, 2, 3],
            Tags = ["a", "b", "c"]
        };

        var struct2 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [1, 2, 3],
            Tags = ["a", "b", "c"]
        };

        await Assert.That(struct1).IsEquivalentTo(struct2);
    }

    [Test]
    public async Task IsEquivalentTo_StructWithReferenceProperties_DifferentArrayContent_ShouldFail()
    {
        var struct1 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [1, 2, 3],
            Tags = ["a", "b", "c"]
        };

        var struct2 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [1, 2, 99],
            Tags = ["a", "b", "c"]
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(struct1).IsEquivalentTo(struct2));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IsEquivalentTo_StructWithReferenceProperties_DifferentListContent_ShouldFail()
    {
        var struct1 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [1, 2, 3],
            Tags = ["a", "b", "c"]
        };

        var struct2 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [1, 2, 3],
            Tags = ["a", "b", "different"]
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(struct1).IsEquivalentTo(struct2));

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Struct Implementing IEquatable Tests

    [Test]
    public async Task IsEquivalentTo_StructWithEquatable_ComparesStructurally_NotByEquals()
    {
        // This struct's Equals() only compares Id, ignoring Data
        // IsEquivalentTo should compare ALL fields structurally
        var struct1 = new StructWithEquatable
        {
            Id = "same-id",
            Data = [1, 2, 3]
        };

        var struct2 = new StructWithEquatable
        {
            Id = "same-id",
            Data = [1, 2, 3]
        };

        // Should succeed because structural comparison finds all fields equal
        await Assert.That(struct1).IsEquivalentTo(struct2);
    }

    [Test]
    public async Task IsEquivalentTo_StructWithEquatable_DifferentData_SameId_ShouldFail()
    {
        // This struct's Equals() would return true (same Id), but
        // IsEquivalentTo should fail because Data is different
        var struct1 = new StructWithEquatable
        {
            Id = "same-id",
            Data = [1, 2, 3]
        };

        var struct2 = new StructWithEquatable
        {
            Id = "same-id",
            Data = [4, 5, 6] // Different data!
        };

        // Should fail because structural comparison finds Data different
        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(struct1).IsEquivalentTo(struct2));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IsEquivalentTo_StructWithEquatable_InTuple_ComparesStructurally()
    {
        var struct1 = new StructWithEquatable { Id = "id1", Data = [1, 2, 3] };
        var struct2 = new StructWithEquatable { Id = "id1", Data = [1, 2, 3] };

        var tuple1 = (struct1, "extra");
        var tuple2 = (struct2, "extra");

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_StructWithEquatable_InTuple_DifferentData_ShouldFail()
    {
        var struct1 = new StructWithEquatable { Id = "id1", Data = [1, 2, 3] };
        var struct2 = new StructWithEquatable { Id = "id1", Data = [9, 9, 9] }; // Different!

        var tuple1 = (struct1, "extra");
        var tuple2 = (struct2, "extra");

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(tuple1).IsEquivalentTo(tuple2));

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Nested Struct Tests

    [Test]
    public async Task IsEquivalentTo_NestedStruct_WithEquivalentContent_ShouldSucceed()
    {
        var struct1 = new NestedStruct
        {
            Name = "Outer",
            Inner = new StructWithReferenceProperties
            {
                Name = "Inner",
                Numbers = [1, 2, 3],
                Tags = ["a", "b"]
            }
        };

        var struct2 = new NestedStruct
        {
            Name = "Outer",
            Inner = new StructWithReferenceProperties
            {
                Name = "Inner",
                Numbers = [1, 2, 3],
                Tags = ["a", "b"]
            }
        };

        await Assert.That(struct1).IsEquivalentTo(struct2);
    }

    [Test]
    public async Task IsEquivalentTo_NestedStruct_DifferentInnerArray_ShouldFail()
    {
        var struct1 = new NestedStruct
        {
            Name = "Outer",
            Inner = new StructWithReferenceProperties
            {
                Name = "Inner",
                Numbers = [1, 2, 3],
                Tags = ["a", "b"]
            }
        };

        var struct2 = new NestedStruct
        {
            Name = "Outer",
            Inner = new StructWithReferenceProperties
            {
                Name = "Inner",
                Numbers = [1, 2, 99], // Different!
                Tags = ["a", "b"]
            }
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(struct1).IsEquivalentTo(struct2));

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Nullable Reference in Struct Tests

    [Test]
    public async Task IsEquivalentTo_StructWithNullableReference_BothNull_ShouldSucceed()
    {
        var struct1 = new StructWithNullableReference
        {
            Name = null,
            Numbers = null
        };

        var struct2 = new StructWithNullableReference
        {
            Name = null,
            Numbers = null
        };

        await Assert.That(struct1).IsEquivalentTo(struct2);
    }

    [Test]
    public async Task IsEquivalentTo_StructWithNullableReference_BothPopulated_ShouldSucceed()
    {
        var struct1 = new StructWithNullableReference
        {
            Name = "Test",
            Numbers = [1, 2, 3]
        };

        var struct2 = new StructWithNullableReference
        {
            Name = "Test",
            Numbers = [1, 2, 3]
        };

        await Assert.That(struct1).IsEquivalentTo(struct2);
    }

    [Test]
    public async Task IsEquivalentTo_StructWithNullableReference_OneNull_ShouldFail()
    {
        var struct1 = new StructWithNullableReference
        {
            Name = "Test",
            Numbers = [1, 2, 3]
        };

        var struct2 = new StructWithNullableReference
        {
            Name = "Test",
            Numbers = null
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(struct1).IsEquivalentTo(struct2));

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region ValueTuple with Non-Record Class Tests

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithNonRecordClass_ShouldSucceed()
    {
        var obj1 = new NonRecordClass { Value = "test", Data = [1, 2, 3] };
        var obj2 = new NonRecordClass { Value = "test", Data = [1, 2, 3] };

        var tuple1 = (obj1, 42);
        var tuple2 = (obj2, 42);

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithNonRecordClass_DifferentData_ShouldFail()
    {
        var obj1 = new NonRecordClass { Value = "test", Data = [1, 2, 3] };
        var obj2 = new NonRecordClass { Value = "test", Data = [4, 5, 6] };

        var tuple1 = (obj1, 42);
        var tuple2 = (obj2, 42);

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(tuple1).IsEquivalentTo(tuple2));

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region Empty Collection Tests

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithEmptyArrays_ShouldSucceed()
    {
        var tuple1 = (Array.Empty<int>(), "test");
        var tuple2 = (Array.Empty<int>(), "test");

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_WithEmptyLists_ShouldSucceed()
    {
        var tuple1 = (new List<string>(), 42);
        var tuple2 = (new List<string>(), 42);

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_EmptyVsNonEmpty_ShouldFail()
    {
        var tuple1 = (Array.Empty<int>(), "test");
        var tuple2 = (new int[] { 1 }, "test");

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(tuple1).IsEquivalentTo(tuple2));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IsEquivalentTo_Struct_WithEmptyCollections_ShouldSucceed()
    {
        var struct1 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [],
            Tags = []
        };

        var struct2 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [],
            Tags = []
        };

        await Assert.That(struct1).IsEquivalentTo(struct2);
    }

    #endregion

    #region Complex Mixed Scenarios

    [Test]
    public async Task IsEquivalentTo_ComplexNestedStructure_ShouldSucceed()
    {
        var person1 = new Person("John", 30, ["developer", "musician"]);
        var thing1 = new Thing("Widget", [100, 200, 300]);

        var tuple1 = ((person1, thing1), new List<int> { 1, 2, 3 }, "metadata");

        var person2 = new Person("John", 30, ["developer", "musician"]);
        var thing2 = new Thing("Widget", [100, 200, 300]);

        var tuple2 = ((person2, thing2), new List<int> { 1, 2, 3 }, "metadata");

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsEquivalentTo_TupleOfStructs_WithDifferentArraySizes_ShouldFail()
    {
        var struct1 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [1, 2, 3],
            Tags = ["a"]
        };

        var struct2 = new StructWithReferenceProperties
        {
            Name = "Test",
            Numbers = [1, 2, 3, 4, 5], // More elements
            Tags = ["a"]
        };

        var tuple1 = (struct1, "extra");
        var tuple2 = (struct2, "extra");

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(tuple1).IsEquivalentTo(tuple2));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task IsEquivalentTo_ListOfTuples_WithEquivalentContent_ShouldSucceed()
    {
        var list1 = new List<(Thing, int)>
        {
            (new Thing("A", [1, 2]), 10),
            (new Thing("B", [3, 4]), 20)
        };

        var list2 = new List<(Thing, int)>
        {
            (new Thing("A", [1, 2]), 10),
            (new Thing("B", [3, 4]), 20)
        };

        await Assert.That(list1).IsEquivalentTo(list2);
    }

    [Test]
    public async Task IsEquivalentTo_ArrayOfStructs_WithEquivalentContent_ShouldSucceed()
    {
        var array1 = new StructWithReferenceProperties[]
        {
            new() { Name = "First", Numbers = [1], Tags = ["a"] },
            new() { Name = "Second", Numbers = [2], Tags = ["b"] }
        };

        var array2 = new StructWithReferenceProperties[]
        {
            new() { Name = "First", Numbers = [1], Tags = ["a"] },
            new() { Name = "Second", Numbers = [2], Tags = ["b"] }
        };

        await Assert.That(array1).IsEquivalentTo(array2);
    }

    [Test]
    public async Task IsEquivalentTo_DictionaryInTuple_WithEquivalentContent_ShouldSucceed()
    {
        var dict1 = new Dictionary<string, int[]>
        {
            ["key1"] = [1, 2, 3],
            ["key2"] = [4, 5, 6]
        };

        var dict2 = new Dictionary<string, int[]>
        {
            ["key1"] = [1, 2, 3],
            ["key2"] = [4, 5, 6]
        };

        var tuple1 = (dict1, "metadata");
        var tuple2 = (dict2, "metadata");

        await Assert.That(tuple1).IsEquivalentTo(tuple2);
    }

    #endregion

    #region ValueTuple Same Order Same Content Tests

    [Test]
    public async Task IsEquivalentTo_ValueTuple_SameOrderSameContent_ShouldSucceed()
    {
        var foo1 = new Thing("Foo", [1, 2, 3]);
        var bar1 = new Thing("Bar", [4, 5, 6]);
        var foo2 = new Thing("Foo", [1, 2, 3]);
        var bar2 = new Thing("Bar", [4, 5, 6]);

        await Assert.That((foo1, bar1)).IsEquivalentTo((foo2, bar2));
    }

    [Test]
    public async Task IsEquivalentTo_ValueTuple_SwappedElements_DifferentContent_ShouldFail()
    {
        var foo = new Thing("Foo", [1, 2, 3]);
        var bar = new Thing("Bar", [4, 5, 6]);

        // (foo, bar) vs (bar, foo) - different because foo != bar
        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That((foo, bar)).IsEquivalentTo((bar, foo)));

        await Assert.That(exception).IsNotNull();
    }

    #endregion

    #region IsNotEquivalentTo Tests

    [Test]
    public async Task IsNotEquivalentTo_ValueTuple_DifferentContent_ShouldSucceed()
    {
        var tuple1 = (new Thing("A", [1, 2, 3]), 42);
        var tuple2 = (new Thing("B", [4, 5, 6]), 42);

        await Assert.That(tuple1).IsNotEquivalentTo(tuple2);
    }

    [Test]
    public async Task IsNotEquivalentTo_Struct_DifferentContent_ShouldSucceed()
    {
        var struct1 = new StructWithReferenceProperties
        {
            Name = "Test1",
            Numbers = [1, 2, 3],
            Tags = ["a"]
        };

        var struct2 = new StructWithReferenceProperties
        {
            Name = "Test2",
            Numbers = [4, 5, 6],
            Tags = ["b"]
        };

        await Assert.That(struct1).IsNotEquivalentTo(struct2);
    }

    [Test]
    public async Task IsNotEquivalentTo_ValueTuple_SameContent_ShouldFail()
    {
        var tuple1 = (new Thing("A", [1, 2, 3]), 42);
        var tuple2 = (new Thing("A", [1, 2, 3]), 42);

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(tuple1).IsNotEquivalentTo(tuple2));

        await Assert.That(exception).IsNotNull();
    }

    #endregion
}
