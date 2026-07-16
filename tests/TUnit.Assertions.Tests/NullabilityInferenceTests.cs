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

    [Test]
    public async Task NotNull_ValueType_Chaining()
    {
        // ReSharper disable once SuggestVarOrType_BuiltInTypes
        // ReSharper disable once ConvertToConstant.Local
        // ReSharper disable once VariableCanBeNotNullable
        int? nullableValue = 1;
        int nonNullableValue = 1;

        var notNullValue = await Assert.That(nullableValue).IsNotNull().And.IsBetween(0, 10);
        var notNullValue2 = await Assert.That(nonNullableValue).IsNotDefault().And.IsBetween(0, 10);

        Console.WriteLine(notNullValue.ToString());
    }

    [Test]
    public async Task NotNull_Dictionary_Chaining_With_Contains_Lambda()
    {
        // Regression test for issue #3471
        // This should compile and work correctly
        var dictionary = new Dictionary<string, string>
        {
            { "key", "value" }
        };

        // This works fine:
        await Assert.That(dictionary).Contains(x => x.Key == "key");

        // This should also work (was failing to compile before fix):
        await Assert.That(dictionary).IsNotNull().And.Contains(x => x.Key == "key");
    }

    [Test]
    public async Task NotNull_List_Chaining_With_Contains_Lambda()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Verify that lambda type inference works after IsNotNull().And for lists
        await Assert.That(list).IsNotNull().And.Contains(x => x > 3);
    }
}
