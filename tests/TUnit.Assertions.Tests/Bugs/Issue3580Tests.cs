namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Tests for issue #3580: More warnings CS8620 and CS8619 after update to >= 0.72
/// https://github.com/thomhurst/TUnit/issues/3580
/// </summary>
public class Issue3580Tests
{
    [Test]
    public async Task Task_FromResult_With_NonNullable_Value_And_Nullable_Expected_Should_Not_Cause_Warnings()
    {
        // Scenario 1: Both parameters nullable (from issue)
        object? value = "test";
        object? expected = "test";

        // This should not produce CS8620 or CS8619 warnings
        await Assert.That(Task.FromResult(value)).IsEqualTo(expected);
    }

    [Test]
    public async Task Task_FromResult_With_NonNullable_Value_And_NonNullable_Expected_Should_Not_Cause_Warnings()
    {
        // Scenario 2: Non-nullable value with nullable expectation (from issue)
        object value = "test";
        object? expected = "test";

        // This should not produce CS8620 or CS8619 warnings
        await Assert.That(Task.FromResult(value)).IsEqualTo(expected);
    }

    [Test]
    public async Task Task_FromResult_With_Both_NonNullable_Should_Not_Cause_Warnings()
    {
        // Scenario 3: Both non-nullable (from issue)
        object value = "test";
        object expected = "test";

        // This should not produce CS8619 warnings
        await Assert.That(Task.FromResult(value)).IsEqualTo(expected);
    }

    [Test]
    public async Task Task_FromResult_With_String_Should_Not_Cause_Warnings()
    {
        // Test with string type (reference type)
        var value = "hello";
        var expected = "hello";

        await Assert.That(Task.FromResult(value)).IsEqualTo(expected);
    }

    [Test]
    public async Task Task_FromResult_With_ValueType_Should_Not_Cause_Warnings()
    {
        // Test with value type (int)
        var value = 42;
        var expected = 42;

        await Assert.That(Task.FromResult(value)).IsEqualTo(expected);
    }

    [Test]
    public async Task Task_FromResult_With_Null_Value_Should_Work()
    {
        // Test with null value
        object? value = null;

        var result = await Task.FromResult(value);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Task_FromResult_With_Custom_Type_Should_Not_Cause_Warnings()
    {
        // Test with custom type
        var value = new TestData { Value = "test" };
        var expected = value;

        var result = await Task.FromResult(value);
        await Assert.That(result).IsSameReferenceAs(expected);
    }

    [Test]
    public async Task Async_Method_Returning_NonNullable_Task_Should_Not_Cause_Warnings()
    {
        // Test with async method that returns non-nullable task
        // Just verify we can await it and get a non-null result
        var result = await GetNonNullableValueAsync();

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Task_FromResult_With_IsNotNull_Should_Not_Cause_Warnings()
    {
        // Test IsNotNull assertion
        object value = "test";

        var result = await Task.FromResult(value);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Task_FromResult_With_IsGreaterThan_Should_Not_Cause_Warnings()
    {
        // Test numeric comparison
        var value = 10;

        await Assert.That(Task.FromResult(value)).IsGreaterThan(5);
    }

    private static async Task<object> GetNonNullableValueAsync()
    {
        await Task.Yield();
        return new object();
    }

    private class TestData
    {
        public string Value { get; set; } = string.Empty;
    }
}
