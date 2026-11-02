using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7225;

/// <summary>
/// [InstanceMethodDataSource] read every member by reflection in source-gen mode before #7225, so its generated
/// factory must keep that behavior: an <c>object</c> value holding many rows supplies each row, a private method
/// is still callable, and a single tuple parameter still receives the whole tuple.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class InstanceMethodDataSourceMemberShapeTests
{
    public object ObjectProperty => new[] { 61, 62 };

    public object ObjectMethod() => new[] { 63, 64 };

    public IEnumerable<(int, int)> TupleRows => [(1, 2), (3, 4)];

    private IEnumerable<int> PrivateRows() => [65, 66];

    [Test]
    [InstanceMethodDataSource(nameof(ObjectProperty))]
    public async Task Object_Property(int value)
    {
        await Assert.That(value).IsBetween(61, 62);
    }

    [Test]
    [InstanceMethodDataSource(nameof(ObjectMethod))]
    public async Task Object_Method(int value)
    {
        await Assert.That(value).IsBetween(63, 64);
    }

    [Test]
    [InstanceMethodDataSource(nameof(TupleRows))]
    public async Task Tuple_Single_Parameter((int, int) value)
    {
        await Assert.That(value.Item2 - value.Item1).IsEqualTo(1);
    }

    [Test]
    [InstanceMethodDataSource(nameof(PrivateRows))]
    public async Task Private_Method(int value)
    {
        await Assert.That(value).IsBetween(65, 66);
    }
}
