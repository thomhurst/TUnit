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
}
