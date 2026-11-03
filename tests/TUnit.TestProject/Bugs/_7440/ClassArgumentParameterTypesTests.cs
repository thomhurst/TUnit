using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7440;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[IntRows7440]
public class ClassArgumentParameterTypesTests<T>(T value)
{
    [Test]
    [Arguments(2)]
    public async Task Run(decimal number)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value is int actual && (actual == 1 || actual == 7)).IsTrue();
        await Assert.That(number).IsEqualTo(2m);
    }
}

public class IntRows7440Attribute : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
        => [() => 7];
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(3)]
[Arguments(3L)]
[Arguments(int.MaxValue)]
public class DecimalConstructorArgumentTests(decimal value)
{
    [Test]
    [Arguments(4)]
    public async Task Run(int number)
    {
        await Assert.That(value == 3m || value == int.MaxValue).IsTrue();
        await Assert.That(number).IsEqualTo(4);
    }
}

public static class DecimalArgumentConstants
{
    public const double DoubleRate = 0.1;
    public const float FloatRate = 0.1f;
    public const string Amount = "3\n";
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(DecimalArgumentConstants.DoubleRate)]
[Arguments(DecimalArgumentConstants.FloatRate)]
public class DecimalConstantConstructorArgumentTests(decimal value)
{
    [Test]
    [Arguments(DecimalArgumentConstants.DoubleRate)]
    [Arguments(DecimalArgumentConstants.FloatRate)]
    public async Task Run(decimal number)
    {
        await Assert.That(value).IsEqualTo(0.1m);
        await Assert.That(number).IsEqualTo(0.1m);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(DecimalArgumentConstants.Amount)]
public class DecimalStringConstantConstructorArgumentTests(decimal value)
{
    [Test]
    [Arguments(DecimalArgumentConstants.Amount)]
    public async Task Run(decimal number)
    {
        await Assert.That(value).IsEqualTo(3m);
        await Assert.That(number).IsEqualTo(3m);
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(3L, 1)]
[Arguments(4L, 2)]
[Arguments(DecimalArgumentConstants.DoubleRate, 3)]
[Arguments(DecimalArgumentConstants.FloatRate, 4)]
[Arguments(DecimalArgumentConstants.Amount, 5)]
public class GenericDecimalConstructorArgumentTests<T>(decimal amount, T value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        var row = (int)(object)value!;
        await Assert.That(amount).IsEqualTo(row switch
        {
            1 => 3m,
            2 => 4m,
            3 or 4 => 0.1m,
            _ => 3m
        });
    }
}
