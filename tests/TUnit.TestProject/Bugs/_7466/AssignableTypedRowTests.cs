using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7466;

[EngineTest(ExpectedResult.Pass)]
public class AssignableTypedRowTests
{
    [Test]
    [Arguments<int[]>(new[] { 1, 2 })]
    public async Task Array<T>(IEnumerable<T> values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Count()).IsEqualTo(2);
    }

    [Test]
    [AssignableRow]
    public async Task InheritedShapes<T>(IEnumerable<T> values, RowBase<T> item, IMarker<T> marker)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Count()).IsEqualTo(2);
        await Assert.That(item).IsNotNull();
        await Assert.That(marker).IsNotNull();
    }
}

public class RowBase<T>;
public class DerivedRow : RowBase<int>;
public interface IMarker<T>;
public interface ILeft : IMarker<int>;
public interface IRight : IMarker<int>;
public class DiamondRow : ILeft, IRight;

public class AssignableRowAttribute : TypedDataSourceAttribute<(List<int>, DerivedRow, DiamondRow)>
{
    public override async IAsyncEnumerable<Func<Task<(List<int>, DerivedRow, DiamondRow)>>> GetTypedDataRowsAsync(DataGeneratorMetadata metadata)
    {
        await Task.Yield();
        yield return () => Task.FromResult((new List<int> { 1, 2 }, new DerivedRow(), new DiamondRow()));
    }
}
