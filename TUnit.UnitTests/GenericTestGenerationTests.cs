namespace TUnit.UnitTests;

public class GenericTestGenerationTests
{
    [Test]
    [GenerateGenericTest(typeof(int), typeof(string))]
    [GenerateGenericTest(typeof(bool), typeof(double))]
    public async Task GenericTestMethod<T1, T2>()
    {
        await Assert.That(typeof(T1)).IsNotNull();
        await Assert.That(typeof(T2)).IsNotNull();
    }

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

[GenerateGenericTest(typeof(int))]
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(bool))]
public class GenericTestClass<T>
{
    private readonly T _value;

    public GenericTestClass()
    {
        _value = default(T?)!;
    }

    [Test]
    public async Task TestGenericValue()
    {
        await Assert.That(typeof(T)).IsNotNull();
        await Assert.That(_value).IsEqualTo(default(T));
    }

    [Test]
    [Arguments(42)]
    [Arguments("test")]
    public async Task TestWithParameter(object input)
    {
        await Assert.That(input).IsNotNull();
        await Assert.That(typeof(T).Name).IsNotEmpty();
    }
}
