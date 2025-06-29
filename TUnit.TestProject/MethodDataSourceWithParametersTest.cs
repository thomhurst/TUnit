using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class MethodDataSourceWithParametersTest
{
    [Test]
    [MethodDataSource(nameof(GetDataWithParameters), Arguments = new object[] { 5, "test" })]
    public async Task TestWithParameterizedDataSource(int value, string text)
    {
        await Assert.That(value).IsGreaterThan(0);
        await Assert.That(text).IsNotNull();
    }
    
    public static IEnumerable<object[]> GetDataWithParameters(int multiplier, string prefix)
    {
        yield return new object[] { 1 * multiplier, $"{prefix}_1" };
        yield return new object[] { 2 * multiplier, $"{prefix}_2" };
        yield return new object[] { 3 * multiplier, $"{prefix}_3" };
    }

    [Test]
    [MethodDataSource(typeof(DataProviders), nameof(DataProviders.GetDataWithThreeParams), Arguments = new object[] { 10, "hello", true })]
    public async Task TestWithExternalParameterizedDataSource(int number, string message, bool flag)
    {
        await Assert.That(number).IsEqualTo(10);
        await Assert.That(message).IsEqualTo("hello_modified");
        await Assert.That(flag).IsTrue();
    }
}

public static class DataProviders
{
    public static IEnumerable<object[]> GetDataWithThreeParams(int baseNumber, string baseText, bool baseFlag)
    {
        if (baseFlag)
        {
            yield return new object[] { baseNumber, $"{baseText}_modified", baseFlag };
        }
        else
        {
            yield return new object[] { baseNumber * 2, $"{baseText}_doubled", baseFlag };
        }
    }
}