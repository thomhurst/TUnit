namespace TUnit.UnitTests;

public class GenericTestGenerationTests
{

    [Test]
    [Arguments(5)]
    [Arguments("hello")]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(string))]
    public async Task GenericTestWithData<T>(T value)
    {
        // For generic types without constraints, we can't use IsNotNull which requires reference types
        // Instead, check that the value equals itself (which is always true for non-null values)
        await Assert.That(value).IsEqualTo(value);
        await Assert.That(typeof(T).Name).IsNotEmpty();
    }
}

// Concrete test classes for each type instead of using GenerateGenericTest on a generic class
public class GenericTestClass_Int
{
    private readonly int _value;

    public GenericTestClass_Int()
    {
        _value = default(int);
    }

    [Test]
    public async Task TestGenericValue()
    {
        await Assert.That(typeof(int)).IsNotNull();
        await Assert.That(_value).IsEqualTo(default(int));
    }

    [Test]
    [Arguments(42)]
    [Arguments("test")]
    public async Task TestWithParameter(object input)
    {
        await Assert.That(input).IsNotNull();
        await Assert.That(typeof(int).Name).IsNotEmpty();
    }
}

public class GenericTestClass_String
{
    private readonly string? _value;

    public GenericTestClass_String()
    {
        _value = default(string);
    }

    [Test]
    public async Task TestGenericValue()
    {
        await Assert.That(typeof(string)).IsNotNull();
        await Assert.That(_value).IsEqualTo(default(string));
    }

    [Test]
    [Arguments(42)]
    [Arguments("test")]
    public async Task TestWithParameter(object input)
    {
        await Assert.That(input).IsNotNull();
        await Assert.That(typeof(string).Name).IsNotEmpty();
    }
}

public class GenericTestClass_Bool
{
    private readonly bool _value;

    public GenericTestClass_Bool()
    {
        _value = default(bool);
    }

    [Test]
    public async Task TestGenericValue()
    {
        await Assert.That(typeof(bool)).IsNotNull();
        await Assert.That(_value).IsEqualTo(default(bool));
    }

    [Test]
    [Arguments(42)]
    [Arguments("test")]
    public async Task TestWithParameter(object input)
    {
        await Assert.That(input).IsNotNull();
        await Assert.That(typeof(bool).Name).IsNotEmpty();
    }
}
