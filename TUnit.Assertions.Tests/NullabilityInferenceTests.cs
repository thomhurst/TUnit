namespace TUnit.Assertions.Tests;

public class NullabilityInferenceTests
{
    [Test]
    public async Task NotNull_ReferenceType()
    {
        // ReSharper disable once SuggestVarOrType_BuiltInTypes
        // ReSharper disable once ConvertToConstant.Local
        // ReSharper disable once VariableCanBeNotNullable
        string? nullableValue = "Hello World!";

        var notNullValue = await Assert.That(nullableValue).IsNotNull();

        Console.WriteLine(notNullValue.Clone());
    }

    [Test]
    public async Task NotNull_ValueType()
    {
        // ReSharper disable once SuggestVarOrType_BuiltInTypes
        // ReSharper disable once ConvertToConstant.Local
        // ReSharper disable once VariableCanBeNotNullable
        int? nullableValue = 1;

        var notNullValue = await Assert.That(nullableValue).IsNotNull();

        Console.WriteLine(notNullValue.ToString());
    }
}
