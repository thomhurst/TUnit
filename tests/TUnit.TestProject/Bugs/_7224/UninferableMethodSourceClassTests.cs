using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7224Unsupported;

[EngineTest(ExpectedResult.Failure)]
public class UninferableMethodSourceClassTests<T>
{
    public static IEnumerable<object> Rows() => [1];

    [Test, MethodDataSource(nameof(Rows))]
    public void CannotInfer(T value) { }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class ExplicitMethodSourceClassTests<T>
{
    public static IEnumerable<object> Rows() => [1];

    [Test, MethodDataSource(nameof(Rows))]
    public async Task ExplicitTypes(T value) => await Assert.That(value).IsEqualTo((T)(object)1);
}

[EngineTest(ExpectedResult.Failure)]
public class ConstrainedMethodSourceClassTests<T> where T : class
{
    public static IEnumerable<int> Rows() => [1];

    [Test, MethodDataSource(nameof(Rows))]
    public void InvalidClassType(T value) { }
}
