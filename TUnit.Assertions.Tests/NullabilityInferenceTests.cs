namespace TUnit.Assertions.Tests;

public class NullabilityInferenceTests
{
    [Test]
    public async Task NotNull()
    {
        // ReSharper disable once SuggestVarOrType_BuiltInTypes
        // ReSharper disable once ConvertToConstant.Local
        // ReSharper disable once VariableCanBeNotNullable
        string? nullableValue = "Hello World!";

        var notNullValue = await Assert.That(nullableValue).IsNotNull();
        
        Console.WriteLine(notNullValue.Clone());
    }
}