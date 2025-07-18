using TUnit.Core;

namespace TUnit.TestProject;

public class GenericMethodDataSourceTests
{
    [Test]
    [MethodDataSource(nameof(GetData))]
    public async Task GenericTest<T>(T value)
    {
        await Assert.That(value).IsNotNull();
    }
    
    public static IEnumerable<T> GetData<T>()
    {
        yield return (T)(object)42;
        yield return (T)(object)"hello";
    }
}

public class GenericClassMethodDataSourceTests<T>
{
    [Test]
    [MethodDataSource(nameof(GetClassData))]
    public async Task TestWithMethodData(T value)
    {
        await Assert.That(value).IsNotNull();
    }
    
    public static IEnumerable<int> GetClassData()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }
}