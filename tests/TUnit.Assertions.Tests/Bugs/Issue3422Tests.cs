namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Tests for issue #3422: CS8620 and CS8619 warnings with object? parameters
/// https://github.com/thomhurst/TUnit/issues/3422
/// </summary>
public class Issue3422Tests
{
    [Test]
    [Arguments(null, null)]
    [Arguments("test", "test")]
    [Arguments(123, 123)]
    public async Task Assert_That_With_Object_Nullable_Parameters_Should_Not_Cause_Warnings(object? value, object? expected)
    {
        // This should not produce CS8620 or CS8619 warnings
        await Assert.That(value).IsEqualTo(expected);
    }

    [Test]
    public async Task Assert_That_Throws_With_Null_Object_Parameter_Should_Not_Cause_Warnings()
    {
        // This test reproduces the second scenario from issue #3422
        object? data = null;
        await Assert.That(() => MethodToTest(data!)).Throws<ArgumentNullException>();

        Task<object> MethodToTest(object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            return Task.FromResult(new object());
        }
    }

    [Test]
    public async Task Assert_That_With_Non_Null_Object_Parameter_Should_Not_Throw()
    {
        // Test that when data is not null, the method executes successfully without warnings
        object? data = "test";
        var result = await MethodToTest(data!);

        await Assert.That(result).IsNotNull();

        async Task<object> MethodToTest(object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            return await Task.FromResult(new object());
        }
    }
}
