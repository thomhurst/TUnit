using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7158;

public class Gen7158A : IDisposable
{
    public void Dispose()
    {
    }
}

public class Gen7158B : IDisposable
{
    public void Dispose()
    {
    }
}

/// <summary>
/// Each [GenerateGenericTest] instantiation runs only the typed ClassDataSource&lt;T&gt; whose type
/// binds to its type argument, in both modes (#7158).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestTypedClassDataSourceTests
{
    [Test]
    [GenerateGenericTest(typeof(Gen7158A))]
    [GenerateGenericTest(typeof(Gen7158B))]
    [ClassDataSource<Gen7158A>(Shared = SharedType.None)]
    [ClassDataSource<Gen7158B>(Shared = SharedType.None)]
    public async Task Run<T>(T value)
    {
        await Assert.That(value).IsTypeOf<T>();
    }
}
