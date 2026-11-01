using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7197;

// A T? parameter with 'where T : struct' is Nullable<T>. The row infers T = int from the first value, and the
// null converts to int?. The generated code used to reference the open T outside the method and fail to compile.
[EngineTest(ExpectedResult.Pass)]
public class NullableValueTypeGenericMethod
{
    [Test]
    [Arguments(1, null)]
    public async Task Run<T>(T value, T? other) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(other.HasValue).IsFalse();
    }
}
