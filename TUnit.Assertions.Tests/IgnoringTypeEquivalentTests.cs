namespace TUnit.Assertions.Tests;

public class IgnoringTypeEquivalentTests
{
    [Test]
    public async Task IgnoringType_Generic_DateTime_Properties_Are_Ignored()
    {
        var object1 = new MyClassWithDates
        {
            Name = "Test",
            CreatedDate = new DateTime(2023, 1, 1),
            ModifiedDate = new DateTime(2023, 1, 2),
            Value = 123
        };

        var object2 = new MyClassWithDates
        {
            Name = "Test",
            CreatedDate = new DateTime(2024, 6, 15),
            ModifiedDate = new DateTime(2024, 7, 20),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType<DateTime>();
    }

    [Test]
    public async Task IgnoringType_NonGeneric_DateTime_Properties_Are_Ignored()
    {
        var object1 = new MyClassWithDates
        {
            Name = "Test",
            CreatedDate = new DateTime(2023, 1, 1),
            ModifiedDate = new DateTime(2023, 1, 2),
            Value = 123
        };

        var object2 = new MyClassWithDates
        {
            Name = "Test",
            CreatedDate = new DateTime(2024, 6, 15),
            ModifiedDate = new DateTime(2024, 7, 20),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType(typeof(DateTime));
    }

    [Test]
    public async Task IgnoringType_Nullable_DateTime_Properties_Are_Ignored()
    {
        var object1 = new MyClassWithNullableDates
        {
            Name = "Test",
            OptionalDate = new DateTime(2023, 1, 1),
            Value = 123
        };

        var object2 = new MyClassWithNullableDates
        {
            Name = "Test",
            OptionalDate = new DateTime(2024, 6, 15),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType<DateTime>();
    }

    [Test]
    public async Task IgnoringType_DateTimeOffset_Properties_Are_Ignored()
    {
        var object1 = new MyClassWithDateTimeOffset
        {
            Name = "Test",
            Timestamp = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Value = 123
        };

        var object2 = new MyClassWithDateTimeOffset
        {
            Name = "Test",
            Timestamp = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.FromHours(2)),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType<DateTimeOffset>();
    }

    [Test]
    public async Task IgnoringType_Multiple_Types_Can_Be_Ignored()
    {
        var object1 = new MyClassWithMultipleTypes
        {
            Name = "Test",
            CreatedDate = new DateTime(2023, 1, 1),
            Guid = Guid.NewGuid(),
            Value = 123
        };

        var object2 = new MyClassWithMultipleTypes
        {
            Name = "Test",
            CreatedDate = new DateTime(2024, 6, 15),
            Guid = Guid.NewGuid(),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType<DateTime>()
            .IgnoringType<Guid>();
    }

    [Test]
    public async Task IgnoringType_Fields_Are_Also_Ignored()
    {
        var object1 = new MyClassWithDateFields
        {
            Name = "Test",
            CreatedDateField = new DateTime(2023, 1, 1),
            Value = 123
        };

        var object2 = new MyClassWithDateFields
        {
            Name = "Test",
            CreatedDateField = new DateTime(2024, 6, 15),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType<DateTime>();
    }

    [Test]
    public async Task NotEquivalentTo_IgnoringType_Works_Correctly()
    {
        var object1 = new MyClassWithDates
        {
            Name = "Different",
            CreatedDate = new DateTime(2023, 1, 1),
            ModifiedDate = new DateTime(2023, 1, 2),
            Value = 123
        };

        var object2 = new MyClassWithDates
        {
            Name = "Test",
            CreatedDate = new DateTime(2024, 6, 15),
            ModifiedDate = new DateTime(2024, 7, 20),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsNotEquivalentTo(object2)
            .IgnoringType<DateTime>();
    }

    [Test]
    public async Task IgnoringType_Without_Matching_Type_Still_Compares_All_Properties()
    {
        var object1 = new MyClassWithoutDates
        {
            Name = "Test",
            Value = 123
        };

        var object2 = new MyClassWithoutDates
        {
            Name = "Different",
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsNotEquivalentTo(object2)
            .IgnoringType<DateTime>();
    }

    private class MyClassWithDates
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int Value { get; set; }
    }

    private class MyClassWithNullableDates
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? OptionalDate { get; set; }
        public int Value { get; set; }
    }

    private class MyClassWithDateTimeOffset
    {
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public int Value { get; set; }
    }

    private class MyClassWithMultipleTypes
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public Guid Guid { get; set; }
        public int Value { get; set; }
    }

    private class MyClassWithDateFields
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDateField;
        public int Value { get; set; }
    }

    private class MyClassWithoutDates
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    // Test classes for ValueType/Tuple tests
    private class IgnoreMe
    {
        public string Message { get; set; } = string.Empty;

        public IgnoreMe() { }
        public IgnoreMe(string message) => Message = message;
    }

    private class ClassWithTupleProperty
    {
        public string Name { get; set; } = string.Empty;
        public (IgnoreMe, IgnoreMe) Ignores { get; set; }
        public int Value { get; set; }
    }

    private class ClassWithNestedTupleProperty
    {
        public string Name { get; set; } = string.Empty;
        public ((IgnoreMe, int), string) NestedIgnores { get; set; }
        public int Value { get; set; }
    }

    private class ClassWithMixedTupleProperty
    {
        public string Name { get; set; } = string.Empty;
        public (IgnoreMe, int) MixedTuple { get; set; }
        public int Value { get; set; }
    }

    [Test]
    public async Task IgnoringType_In_Tuple_Properties_Are_Ignored()
    {
        var object1 = new ClassWithTupleProperty
        {
            Name = "Test",
            Ignores = (new IgnoreMe("foobar"), new IgnoreMe("foobar")),
            Value = 123
        };

        var object2 = new ClassWithTupleProperty
        {
            Name = "Test",
            Ignores = (new IgnoreMe("baz"), new IgnoreMe("baz")),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType<IgnoreMe>();
    }

    [Test]
    public async Task IgnoringType_In_Nested_Tuple_Properties_Are_Ignored()
    {
        var object1 = new ClassWithNestedTupleProperty
        {
            Name = "Test",
            NestedIgnores = ((new IgnoreMe("foobar"), 1), "hello"),
            Value = 123
        };

        var object2 = new ClassWithNestedTupleProperty
        {
            Name = "Test",
            NestedIgnores = ((new IgnoreMe("baz"), 1), "hello"),
            Value = 123
        };

        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType<IgnoreMe>();
    }

    [Test]
    public async Task IgnoringType_In_Mixed_Tuple_Still_Compares_Non_Ignored_Parts()
    {
        var object1 = new ClassWithMixedTupleProperty
        {
            Name = "Test",
            MixedTuple = (new IgnoreMe("foobar"), 42),
            Value = 123
        };

        var object2 = new ClassWithMixedTupleProperty
        {
            Name = "Test",
            MixedTuple = (new IgnoreMe("baz"), 99), // Different int value
            Value = 123
        };

        // Should fail because the int part (42 vs 99) is different
        await TUnitAssert.That(object1)
            .IsNotEquivalentTo(object2)
            .IgnoringType<IgnoreMe>();
    }

    [Test]
    public async Task IgnoringType_In_Mixed_Tuple_Passes_When_NonIgnored_Parts_Match()
    {
        var object1 = new ClassWithMixedTupleProperty
        {
            Name = "Test",
            MixedTuple = (new IgnoreMe("foobar"), 42),
            Value = 123
        };

        var object2 = new ClassWithMixedTupleProperty
        {
            Name = "Test",
            MixedTuple = (new IgnoreMe("baz"), 42), // Same int value
            Value = 123
        };

        // Should pass because the int part is the same and IgnoreMe is ignored
        await TUnitAssert.That(object1)
            .IsEquivalentTo(object2)
            .IgnoringType<IgnoreMe>();
    }
}