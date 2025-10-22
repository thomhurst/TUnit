namespace TUnit.Assertions.Tests.Old;

public class DefaultAssertionTests
{
    [Test]
    public async Task IsDefault_ValueType_Integer_Default()
    {
        var x = 0;
        await TUnitAssert.That(x).IsDefault();
    }

    [Test]
    public async Task IsDefault_ValueType_Integer_NotDefault()
    {
        await TUnitAssert.That(async () =>
        {
            var x = 1;
            await TUnitAssert.That(x).IsDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsDefault_ValueType_DateTime_Default()
    {
        DateTime dt = default;
        await TUnitAssert.That(dt).IsDefault();
    }

    [Test]
    public async Task IsDefault_ValueType_DateTime_NotDefault()
    {
        await TUnitAssert.That(async () =>
        {
            var dt = DateTime.Now;
            await TUnitAssert.That(dt).IsDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_ValueType_Integer_Default()
    {
        await TUnitAssert.That(async () =>
        {
            var x = 0;
            await TUnitAssert.That(x).IsNotDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_ValueType_Integer_NotDefault()
    {
        var x = 1;
        await TUnitAssert.That(x).IsNotDefault();
    }

    [Test]
    public async Task IsNotDefault_ValueType_DateTime_Default()
    {
        await TUnitAssert.That(async () =>
        {
            DateTime dt = default;
            await TUnitAssert.That(dt).IsNotDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_ValueType_DateTime_NotDefault()
    {
        var dt = DateTime.Now;
        await TUnitAssert.That(dt).IsNotDefault();
    }

    [Test]
    public async Task IsDefault_ReferenceType_String_Null()
    {
        string? str = null;
        await TUnitAssert.That(str).IsDefault();
    }

    [Test]
    public async Task IsDefault_ReferenceType_String_NotNull()
    {
        await TUnitAssert.That(async () =>
        {
            string str = "Hello";
            await TUnitAssert.That(str).IsDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsDefault_ReferenceType_Object_Null()
    {
        object? obj = null;
        await TUnitAssert.That(obj).IsDefault();
    }

    [Test]
    public async Task IsDefault_ReferenceType_Object_NotNull()
    {
        await TUnitAssert.That(async () =>
        {
            object obj = new object();
            await TUnitAssert.That(obj).IsDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_ReferenceType_String_Null()
    {
        await TUnitAssert.That(async () =>
        {
            string? str = null;
            await TUnitAssert.That(str).IsNotDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_ReferenceType_String_NotNull()
    {
        string str = "Hello";
        await TUnitAssert.That(str).IsNotDefault();
    }

    [Test]
    public async Task IsNotDefault_ReferenceType_String_Empty()
    {
        string str = "";
        await TUnitAssert.That(str).IsNotDefault();
    }

    [Test]
    public async Task IsNotDefault_ReferenceType_Object_Null()
    {
        await TUnitAssert.That(async () =>
        {
            object? obj = null;
            await TUnitAssert.That(obj).IsNotDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_ReferenceType_Object_NotNull()
    {
        object obj = new object();
        await TUnitAssert.That(obj).IsNotDefault();
    }

    // ============ NULLABLE VALUE TYPE TESTS ============

    [Test]
    public async Task IsDefault_NullableValueType_Bool_Null()
    {
        bool? value = null;
        await TUnitAssert.That(value).IsDefault();
    }

    [Test]
    public async Task IsDefault_NullableValueType_Bool_HasValue()
    {
        await TUnitAssert.That(async () =>
        {
            bool? value = false;
            await TUnitAssert.That(value).IsDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsDefault_NullableValueType_Int_Null()
    {
        int? value = null;
        await TUnitAssert.That(value).IsDefault();
    }

    [Test]
    public async Task IsDefault_NullableValueType_Int_HasValue()
    {
        await TUnitAssert.That(async () =>
        {
            int? value = 42;
            await TUnitAssert.That(value).IsDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsDefault_NullableValueType_DateTime_Null()
    {
        DateTime? value = null;
        await TUnitAssert.That(value).IsDefault();
    }

    [Test]
    public async Task IsDefault_NullableValueType_DateTime_HasValue()
    {
        await TUnitAssert.That(async () =>
        {
            DateTime? value = DateTime.Now;
            await TUnitAssert.That(value).IsDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_NullableValueType_Bool_Null()
    {
        await TUnitAssert.That(async () =>
        {
            bool? value = null;
            await TUnitAssert.That(value).IsNotDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_NullableValueType_Bool_True()
    {
        bool? value = true;
        await TUnitAssert.That(value).IsNotDefault();
    }

    [Test]
    public async Task IsNotDefault_NullableValueType_Bool_False()
    {
        bool? value = false;
        await TUnitAssert.That(value).IsNotDefault();
    }

    [Test]
    public async Task IsNotDefault_NullableValueType_Int_Null()
    {
        await TUnitAssert.That(async () =>
        {
            int? value = null;
            await TUnitAssert.That(value).IsNotDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_NullableValueType_Int_Zero()
    {
        int? value = 0;
        await TUnitAssert.That(value).IsNotDefault();
    }

    [Test]
    public async Task IsNotDefault_NullableValueType_Int_NonZero()
    {
        int? value = 42;
        await TUnitAssert.That(value).IsNotDefault();
    }

    [Test]
    public async Task IsNotDefault_NullableValueType_DateTime_Null()
    {
        await TUnitAssert.That(async () =>
        {
            DateTime? value = null;
            await TUnitAssert.That(value).IsNotDefault();
        }).Throws<TUnitAssertionException>();
    }

    [Test]
    public async Task IsNotDefault_NullableValueType_DateTime_HasValue()
    {
        DateTime? value = DateTime.Now;
        await TUnitAssert.That(value).IsNotDefault();
    }
}
