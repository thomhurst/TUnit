using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class GenericTupleDataSourceTests
{
    public static (int, int) Value() => (1, 2);

    [Test]
    [MethodDataSource(nameof(Value))]
    public async Task Tuple_Infers_Generic_Argument<T>((T, T) value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object)value).IsEqualTo((object)(1, 2));
    }
}
